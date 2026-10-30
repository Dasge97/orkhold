// POST /herramientas/fabricar: Ferra fabrica una herramienta de saqueo (08 §5). Se paga de los escondites
// con el coste del balance y queda como unidad hasta que se usa en un saqueo.
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { B } from '../balance.js';
import { leerJugador, sumar, ErrorJuego } from '../inventario.js';
import { HERRAMIENTAS, costeDe, requisito } from '../reglas.js';
import { jefeVencido } from './botin.js';

export default async function (app) {
  /** Con sesión, "disponible" dice si este jugador ya la puede fabricar (jefe vencido). */
  app.get('/herramientas', async (req) => {
    const { leerToken } = await import('../autenticacion.js');
    const cab = req.headers.authorization ?? '';
    const yo = cab.startsWith('Bearer ') ? leerToken(app.opciones.secreto, cab.slice(7))?.j : null;
    const c = await app.pool.connect();
    try {
      const lista = [];
      for (const id of HERRAMIENTAS) {
        const n = requisito(`herramienta.${id}`);
        const abierta = !!B(`herramienta.${id}.disponible`) && (yo ? await jefeVencido(c, yo, n) : n === 0);
        lista.push({ id, disponible: abierta, mazmorra: n, coste: costeDe(`herramienta.${id}`) });
      }
      return { herramientas: lista };
    } finally { c.release(); }
  });

  app.post('/herramientas/fabricar', { config: { sesion: true } }, async (req) => {
    const { id } = validar(z.object({ id: z.enum(HERRAMIENTAS) }), req.body);
    if (!B(`herramienta.${id}.disponible`)) throw new ErrorJuego('herramienta_bloqueada', 'Ferra todavía no sabe hacer eso');
    const coste = costeDe(`herramienta.${id}`);
    return enTransaccion(app.pool, async (c) => {
      const n = requisito(`herramienta.${id}`);
      if (!(await jefeVencido(c, req.jugadorId, n)))
        throw new ErrorJuego('herramienta_bloqueada', `Ferra: "Tráeme lo de la mazmorra ${n} y te lo hago"`, 403);
      const delta = Object.fromEntries(Object.entries(coste).map(([r, n]) => [r, -n]));
      delta[id] = 1;
      try { await sumar(c, req.jugadorId, delta); }
      catch (e) {
        if (e.codigo === 'recursos_insuficientes') throw new ErrorJuego('recursos_insuficientes', `Te falta material: ${Object.entries(coste).map(([r, n]) => `${n} ${r}`).join(' + ')}`, 409);
        throw e;
      }
      return { jugador: await leerJugador(c, req.jugadorId) };
    });
  });
}
