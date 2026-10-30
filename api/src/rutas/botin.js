// Entradas de recursos:
// - POST /mazmorra/resultado (cliente): botín de una entrada, con límites por entrada; el jefe da su botín aquí
//   (la unidad la primera vez; hueso y hierro la primera vez del día; monedas las demás).
// - POST /pueblo/botin (cliente, provisional hasta la fase 7): lo recogido en el pueblo, con límite por minuto.
// - POST /inventario/sumar (servidor de mundo, con clave de servidor).
import { z } from 'zod';
import { validar, esServidorDeMundo } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { leerJugador, sumar, ponerProgreso, ErrorJuego } from '../inventario.js';
import { RECURSOS, maxMazmorra, maxPuebloMinuto } from '../reglas.js';
import { B } from '../balance.js';

const Recursos = z.object(Object.fromEntries(RECURSOS.map(r => [r, z.number().int().min(0).max(1000).default(0)]))).default({});

const Mazmorra = z.object({ jefe_muerto: z.boolean(), recursos: Recursos });
const Pueblo = z.object({ recursos: Recursos });
const Sumar = z.object({
  jugador: z.union([z.string(), z.number()]).transform(String),
  recurso: z.enum(RECURSOS),
  cantidad: z.number().int().min(1).max(100),
  origen: z.string().max(40).default('mundo'),
});

export const CLAVE_JEFE = 'jefe1_muerto';
export const CLAVE_JEFE_DIA = 'jefe1_dia';

export default async function (app) {
  app.post('/mazmorra/resultado', { config: { sesion: true } }, async (req) => {
    const { jefe_muerto, recursos } = validar(Mazmorra, req.body);
    for (const r of RECURSOS)
      if (recursos[r] > maxMazmorra(r)) throw new ErrorJuego('botin_imposible', `Demasiado ${r} para una entrada (máximo ${maxMazmorra(r)})`);
    return enTransaccion(app.pool, async (c) => {
      await c.query('SELECT id FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId]);
      const ultima = (await c.query(
        `SELECT extract(epoch FROM now() - creado)::float8 AS s FROM mazmorras WHERE jugador_id = $1 ORDER BY id DESC LIMIT 1`, [req.jugadorId])).rows[0];
      if (ultima && ultima.s < B('mazmorra.segundos_entre'))
        throw new ErrorJuego('demasiado_seguido', 'Otra entrada a la mazmorra demasiado seguida', 429);
      const delta = { ...recursos };
      let primeraVez = false;
      if (jefe_muerto) {
        const p = (await c.query('SELECT valor FROM progreso WHERE jugador_id = $1 AND clave = $2', [req.jugadorId, CLAVE_JEFE])).rows[0];
        primeraVez = !p;
        if (primeraVez) { delta.orco_jefe = 1; await ponerProgreso(c, req.jugadorId, CLAVE_JEFE, '1'); }
        // 08 §1.2: hueso y hierro la primera vez de cada día; las demás, monedas.
        const hoy = new Date().toISOString().slice(0, 10);
        const dia = (await c.query('SELECT valor FROM progreso WHERE jugador_id = $1 AND clave = $2', [req.jugadorId, CLAVE_JEFE_DIA])).rows[0];
        if (dia?.valor !== hoy) {
          delta.hueso = (delta.hueso ?? 0) + B('mazmorra.jefe_hueso');
          delta.hierro = (delta.hierro ?? 0) + B('mazmorra.jefe_hierro');
          await ponerProgreso(c, req.jugadorId, CLAVE_JEFE_DIA, hoy);
        } else delta.moneda = (delta.moneda ?? 0) + B('mazmorra.jefe_monedas');
      }
      await sumar(c, req.jugadorId, delta);
      await c.query('INSERT INTO mazmorras (jugador_id, jefe_muerto, recursos_json) VALUES ($1, $2, $3)',
        [req.jugadorId, jefe_muerto, JSON.stringify(recursos)]);
      return { primera_vez_jefe: primeraVez, jugador: await leerJugador(c, req.jugadorId) };
    });
  });

  app.post('/pueblo/botin', { config: { sesion: true } }, async (req) => {
    if (!app.opciones.puebloCliente) throw new ErrorJuego('no_disponible', 'El botín del pueblo lo suma el servidor de mundo', 403);
    const { recursos } = validar(Pueblo, req.body);
    return enTransaccion(app.pool, async (c) => {
      await c.query('SELECT id FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId]);
      const previos = (await c.query(
        `SELECT recursos_json FROM botin_pueblo WHERE jugador_id = $1 AND creado > now() - interval '60 seconds'`, [req.jugadorId])).rows;
      for (const r of RECURSOS) {
        const ya = previos.reduce((s, f) => s + (f.recursos_json[r] ?? 0), 0);
        if (ya + recursos[r] > maxPuebloMinuto(r)) throw new ErrorJuego('demasiado_botin', `Demasiado ${r} en un minuto`, 429);
      }
      await sumar(c, req.jugadorId, recursos);
      await c.query('INSERT INTO botin_pueblo (jugador_id, recursos_json) VALUES ($1, $2)', [req.jugadorId, JSON.stringify(recursos)]);
      return { jugador: await leerJugador(c, req.jugadorId) };
    });
  });

  app.post('/inventario/sumar', async (req, reply) => {
    if (!esServidorDeMundo(app, req))
      return reply.code(403).send({ error: 'sin_permiso', mensaje: 'Solo el servidor de mundo' });
    const { jugador, recurso, cantidad } = validar(Sumar, req.body);
    return enTransaccion(app.pool, async (c) => {
      await leerJugador(c, jugador);   // 404 si no existe
      await sumar(c, jugador, { [recurso]: cantidad });
      return { jugador: await leerJugador(c, jugador) };
    });
  });
}
