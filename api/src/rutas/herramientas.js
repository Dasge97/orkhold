// POST /herramientas/fabricar: Ferra fabrica una herramienta de saqueo (08 §5). Se paga de los escondites
// con el coste del balance y queda como unidad hasta que se usa en un saqueo.
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { B } from '../balance.js';
import { leerJugador, sumar, ErrorJuego } from '../inventario.js';
import { HERRAMIENTAS, costeDe } from '../reglas.js';

export default async function (app) {
  app.get('/herramientas', async () => ({
    herramientas: HERRAMIENTAS.map(id => ({ id, disponible: !!B(`herramienta.${id}.disponible`), coste: costeDe(`herramienta.${id}`) })),
  }));

  app.post('/herramientas/fabricar', { config: { sesion: true } }, async (req) => {
    const { id } = validar(z.object({ id: z.enum(HERRAMIENTAS) }), req.body);
    if (!B(`herramienta.${id}.disponible`)) throw new ErrorJuego('herramienta_bloqueada', 'Ferra todavía no sabe hacer eso');
    const coste = costeDe(`herramienta.${id}`);
    return enTransaccion(app.pool, async (c) => {
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
