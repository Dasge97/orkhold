// Asaltos (01-diseno.md §5.2):
// - POST /asalto/buscar: rival de ±100 puntos con fuerte, o un fuerte del sistema; máximo 10 al día.
// - POST /asalto/resultado: comprueba la duración, aplica Elo y recompensas.
// - GET /asaltos/historial: últimos 10 hechos y recibidos.  - GET /ranking: 50 mejores y tu puesto.
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { leerJugador, sumar, ErrorJuego } from '../inventario.js';
import { MAX_ASALTOS_DIA, fuerteSistema, limpiarDiseno, puntosGanar, puntosPerder, recompensa } from '../reglas.js';

const Resultado = z.object({
  asalto_id: z.union([z.string(), z.number()]).transform(String),
  ganado: z.boolean(),
  segundos: z.number().int().min(0).max(90),
  muros_rotos: z.number().int().min(0).max(40),
});

const MARGEN_RANGO = 100;
const MINUTOS_PARA_TERMINAR = 15;

export default async function (app) {
  app.post('/asalto/buscar', { config: { sesion: true } }, async (req) => {
    return enTransaccion(app.pool, async (c) => {
      const yo = (await c.query('SELECT id, puntuacion FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId])).rows[0];
      const hoy = (await c.query(
        `SELECT count(*)::int AS n FROM asaltos WHERE atacante_id = $1 AND creado >= date_trunc('day', now())`, [yo.id])).rows[0].n;
      if (hoy >= MAX_ASALTOS_DIA) throw new ErrorJuego('limite_diario', `Ya has hecho ${MAX_ASALTOS_DIA} asaltos hoy`, 429);

      const rival = (await c.query(
        `SELECT j.id, j.nombre, j.puntuacion, f.diseno_json
           FROM jugadores j JOIN fuertes f ON f.jugador_id = j.id
          WHERE j.id <> $1 AND j.nombre IS NOT NULL
            AND jsonb_array_length(f.diseno_json->'piezas') > 0
            AND abs(j.puntuacion - $2) <= $3
          ORDER BY random() LIMIT 1`, [yo.id, yo.puntuacion, MARGEN_RANGO])).rows[0];

      let defensorId = null, sistema = null, diseno, nombre, puntos;
      if (rival) {
        defensorId = rival.id; diseno = limpiarDiseno(rival.diseno_json); nombre = rival.nombre; puntos = rival.puntuacion;
      } else {
        const s = fuerteSistema(yo.puntuacion);
        sistema = s.nombre; diseno = s.diseno; nombre = `Fuerte del sistema (${s.nombre})`; puntos = s.puntos;
      }
      const a = (await c.query(
        `INSERT INTO asaltos (atacante_id, defensor_id, defensor_sistema, diseno_defensor_json, puntos_atacante_antes)
         VALUES ($1, $2, $3, $4, $5) RETURNING id`, [yo.id, defensorId, sistema, JSON.stringify(diseno), yo.puntuacion])).rows[0];
      return {
        asalto_id: String(a.id),
        rival: { nombre, puntuacion: puntos, sistema: !!sistema },
        diseno,
        asaltos_hoy: hoy + 1,
      };
    });
  });

  app.post('/asalto/resultado', { config: { sesion: true } }, async (req) => {
    const r = validar(Resultado, req.body);
    // Una victoria necesita entre 5 y 90 s; una derrota puede ser antes (rendirse).
    if (r.ganado && (r.segundos < 5 || r.segundos > 90)) throw new ErrorJuego('tiempo_imposible', 'Una victoria dura entre 5 y 90 s');
    return enTransaccion(app.pool, async (c) => {
      const a = (await c.query(
        `SELECT a.*, extract(epoch FROM now() - a.creado)::float8 AS pasados,
                coalesce(d.puntuacion, CASE a.defensor_sistema WHEN 'facil' THEN 900 WHEN 'medio' THEN 1000 ELSE 1100 END) AS puntos_rival
           FROM asaltos a LEFT JOIN jugadores d ON d.id = a.defensor_id
          WHERE a.id = $1 FOR UPDATE OF a`, [r.asalto_id])).rows[0];
      if (!a || String(a.atacante_id) !== String(req.jugadorId)) throw new ErrorJuego('asalto_no_existe', 'Ese asalto no es tuyo', 404);
      if (a.terminado) throw new ErrorJuego('asalto_terminado', 'Ese asalto ya tiene resultado', 409);
      if (a.pasados > MINUTOS_PARA_TERMINAR * 60) throw new ErrorJuego('asalto_caducado', 'Ha pasado demasiado tiempo', 409);
      // No se puede declarar más tiempo jugado del que ha pasado desde que se buscó el rival (3 s de margen).
      if (r.segundos > a.pasados + 3) throw new ErrorJuego('tiempo_imposible', 'Más segundos de los que han pasado');

      const yo = (await c.query('SELECT puntuacion FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId])).rows[0];
      const antes = yo.puntuacion;
      const cambio = r.ganado ? puntosGanar(antes, a.puntos_rival) : puntosPerder(antes, a.puntos_rival);
      const despues = Math.max(0, antes + cambio);
      const premio = recompensa(r.ganado);
      await c.query('UPDATE jugadores SET puntuacion = $2 WHERE id = $1', [req.jugadorId, despues]);
      await sumar(c, req.jugadorId, premio);
      await c.query(
        `UPDATE asaltos SET ganado = $2, segundos = $3, muros_rotos = $4, puntos_atacante_despues = $5,
                recompensa_json = $6, terminado = now() WHERE id = $1`,
        [a.id, r.ganado, r.segundos, r.muros_rotos, despues, JSON.stringify(premio)]);
      return { ganado: r.ganado, recompensa: premio, puntos_antes: antes, puntos_despues: despues, jugador: await leerJugador(c, req.jugadorId) };
    });
  });

  app.get('/asaltos/historial', { config: { sesion: true } }, async (req) => {
    const fila = f => ({
      id: String(f.id), rival: f.rival ?? '', ganado: f.ganado, segundos: f.segundos, muros_rotos: f.muros_rotos,
      puntos: f.puntos_atacante_despues - f.puntos_atacante_antes, creado: f.creado.toISOString(),
    });
    const hechos = (await app.pool.query(
      `SELECT a.*, coalesce(d.nombre, 'Fuerte del sistema (' || a.defensor_sistema || ')') AS rival
         FROM asaltos a LEFT JOIN jugadores d ON d.id = a.defensor_id
        WHERE a.atacante_id = $1 AND a.terminado IS NOT NULL ORDER BY a.id DESC LIMIT 10`, [req.jugadorId])).rows.map(fila);
    const recibidos = (await app.pool.query(
      `SELECT a.*, j.nombre AS rival FROM asaltos a JOIN jugadores j ON j.id = a.atacante_id
        WHERE a.defensor_id = $1 AND a.terminado IS NOT NULL ORDER BY a.id DESC LIMIT 10`, [req.jugadorId])).rows.map(fila);
    return { hechos, recibidos };
  });

  app.get('/ranking', { config: { sesion: true } }, async (req) => {
    const mejores = (await app.pool.query(
      `SELECT id, nombre, puntuacion FROM jugadores WHERE nombre IS NOT NULL ORDER BY puntuacion DESC, id ASC LIMIT 50`)).rows
      .map((f, i) => ({ puesto: i + 1, id: String(f.id), nombre: f.nombre, puntuacion: f.puntuacion }));
    const yo = (await app.pool.query('SELECT id, puntuacion FROM jugadores WHERE id = $1', [req.jugadorId])).rows[0];
    const puesto = (await app.pool.query(
      `SELECT count(*)::int + 1 AS p FROM jugadores WHERE nombre IS NOT NULL AND (puntuacion > $1 OR (puntuacion = $1 AND id < $2))`,
      [yo.puntuacion, yo.id])).rows[0].p;
    return { mejores, tu_puesto: puesto, tu_puntuacion: yo.puntuacion };
  });
}
