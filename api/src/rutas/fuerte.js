// GET /catalogo, GET /fuerte y PUT /fuerte (valida casillas, límites y coste; descuenta o devuelve recursos).
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { leerJugador, leerInventario, sumar, ErrorJuego } from '../inventario.js';
import { catalogo, TODO, coste, errorDiseno, limpiarDiseno } from '../reglas.js';

const Pieza = z.object({ x: z.number().int(), z: z.number().int(), tipo: z.string().max(40), vida_extra: z.number().int().min(0).max(10).default(0) });
const Cuerpo = z.object({ diseno: z.object({ version: z.number().int().optional(), piezas: z.array(Pieza).max(80) }) });

export async function leerDiseno(c, jugadorId) {
  const f = (await c.query('SELECT diseno_json FROM fuertes WHERE jugador_id = $1', [jugadorId])).rows[0];
  return f ? limpiarDiseno(f.diseno_json) : { version: 1, piezas: [] };
}

export default async function (app) {
  app.get('/catalogo', async () => catalogo());

  app.get('/fuerte', { config: { sesion: true } }, async (req) => {
    const c = await app.pool.connect();
    try { return { diseno: await leerDiseno(c, req.jugadorId) }; } finally { c.release(); }
  });

  app.put('/fuerte', { config: { sesion: true } }, async (req) => {
    const { diseno } = validar(Cuerpo, req.body);
    const nuevo = limpiarDiseno(diseno);
    const error = errorDiseno(nuevo);
    if (error) throw new ErrorJuego('diseno_no_valido', error);
    return enTransaccion(app.pool, async (c) => {
      await c.query('SELECT id FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId]);
      const viejo = await leerDiseno(c, req.jugadorId);
      // Se paga la diferencia: lo nuevo cuesta, lo quitado se devuelve.
      const cn = coste(nuevo), cv = coste(viejo);
      const delta = Object.fromEntries(TODO.map(r => [r, cv[r] - cn[r]]));
      const inv = await leerInventario(c, req.jugadorId);
      const faltan = TODO.filter(r => inv[r] + delta[r] < 0);
      if (faltan.length) throw new ErrorJuego('recursos_insuficientes', `No tienes bastante: ${faltan.join(', ')}`, 409);
      await sumar(c, req.jugadorId, delta);
      await c.query(
        `INSERT INTO fuertes (jugador_id, diseno_json) VALUES ($1, $2)
         ON CONFLICT (jugador_id) DO UPDATE SET diseno_json = EXCLUDED.diseno_json, actualizado = now()`,
        [req.jugadorId, JSON.stringify(nuevo)]);
      return { diseno: nuevo, jugador: await leerJugador(c, req.jugadorId) };
    });
  });
}
