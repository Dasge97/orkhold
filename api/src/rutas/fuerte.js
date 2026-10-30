// GET /catalogo, GET /fuerte, PUT /fuerte (piezas) y PUT /fuerte/escondites (recolocar a mano).
// PUT /fuerte valida casillas, límites y coste. El contenido de los escondites se reparte solo: los que siguen
// en su casilla conservan lo suyo; lo de los quitados y lo que se devuelve entra en los demás; lo que cuesta
// sale de ellos (08-formato-y-pvp.md §3.1).
import { z } from 'zod';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { leerJugador, leerInventario, leerDisenoParaCambiar, guardarDiseno, sumar, ErrorJuego } from '../inventario.js';
import { catalogo, TODO, coste, errorDiseno, limpiarDiseno, pieza } from '../reglas.js';
import { jefeVencido } from './botin.js';
import { RECURSOS, esEscondite, vacio, meter, sacar, totales, unidades, errorReparto } from '../escondites.js';

const Pieza = z.object({ x: z.number().int(), z: z.number().int(), tipo: z.string().max(40), vida_extra: z.number().int().min(0).max(10).default(0) });
const Cuerpo = z.object({ diseno: z.object({ version: z.number().int().optional(), piezas: z.array(Pieza).max(120) }) });
const Contenido = z.object(Object.fromEntries(RECURSOS.map(r => [r, z.number().int().min(0).max(100000).default(0)])));
const Reparto = z.object({
  central: Contenido,
  escondites: z.array(z.object({ x: z.number().int(), z: z.number().int(), contenido: Contenido })).max(60),
});

export async function leerDiseno(c, jugadorId) {
  const f = (await c.query('SELECT diseno_json FROM fuertes WHERE jugador_id = $1', [jugadorId])).rows[0];
  return limpiarDiseno(f?.diseno_json ?? { piezas: [] });
}

const clave = q => `${q.x},${q.z},${q.tipo}`;

export default async function (app) {
  app.get('/catalogo', async () => catalogo());

  app.get('/fuerte', { config: { sesion: true } }, async (req) => {
    const c = await app.pool.connect();
    try { return { diseno: await leerDiseno(c, req.jugadorId) }; } finally { c.release(); }
  });

  app.put('/fuerte', { config: { sesion: true } }, async (req) => {
    const { diseno } = validar(Cuerpo, req.body);
    const nuevo = limpiarDiseno(diseno, false);
    const error = errorDiseno(nuevo);
    if (error) throw new ErrorJuego('diseno_no_valido', error);
    return enTransaccion(app.pool, async (c) => {
      await c.query('SELECT id FROM jugadores WHERE id = $1 FOR UPDATE', [req.jugadorId]);
      // Piezas que abre un jefe (11-mazmorras.md §6).
      for (const tipo of new Set(nuevo.piezas.map(q => q.tipo))) {
        const n = pieza(tipo)?.mazmorra ?? 0;
        if (n && !(await jefeVencido(c, req.jugadorId, n)))
          throw new ErrorJuego('pieza_bloqueada', `${pieza(tipo).nombre}: se consigue al vencer al jefe de la mazmorra ${n}`, 403);
      }
      const viejo = await leerDisenoParaCambiar(c, req.jugadorId);
      // El contenido sigue en los escondites que no se mueven; el de los quitados queda suelto.
      const antes = new Map(viejo.piezas.filter(q => esEscondite(q.tipo)).map(q => [clave(q), q.contenido]));
      nuevo.central = viejo.central;
      for (const q of nuevo.piezas)
        if (esEscondite(q.tipo)) { q.contenido = antes.get(clave(q)) ?? vacio(); antes.delete(clave(q)); }
      const suelto = vacio();
      for (const cont of antes.values()) for (const r of RECURSOS) suelto[r] += cont[r];

      // Se paga la diferencia: lo nuevo cuesta, lo quitado se devuelve.
      const cn = coste(nuevo), cv = coste(viejo);
      const neto = Object.fromEntries(TODO.map(r => [r, cv[r] - cn[r] + (suelto[r] ?? 0)]));
      const faltan = sacar(nuevo, Object.fromEntries(RECURSOS.map(r => [r, Math.max(0, -neto[r])])));
      if (faltan.length) throw new ErrorJuego('recursos_insuficientes', `No tienes bastante: ${faltan.join(', ')}`, 409);
      const { sobrante } = meter(nuevo, Object.fromEntries(RECURSOS.map(r => [r, Math.max(0, neto[r])])));
      if (unidades(sobrante) > 0)
        throw new ErrorJuego('escondites_llenos', 'Escondites llenos: lo que se devuelve no cabe. Construye otro escondite o gasta antes', 409);
      // Las unidades (orco jefe) van aparte.
      const inv = await leerInventario(c, req.jugadorId);
      const unidadesFaltan = TODO.filter(r => !RECURSOS.includes(r) && inv[r] + neto[r] < 0);
      if (unidadesFaltan.length) throw new ErrorJuego('recursos_insuficientes', `No tienes bastante: ${unidadesFaltan.join(', ')}`, 409);
      await guardarDiseno(c, req.jugadorId, nuevo);
      await sumar(c, req.jugadorId, Object.fromEntries(TODO.filter(r => !RECURSOS.includes(r)).map(r => [r, neto[r]])));
      return { diseno: limpiarDiseno(nuevo), jugador: await leerJugador(c, req.jugadorId) };
    });
  });

  /** Recolocar a mano: el reparto nuevo tiene que tener lo mismo que antes y caber en cada escondite. */
  app.put('/fuerte/escondites', { config: { sesion: true } }, async (req) => {
    const r = validar(Reparto, req.body);
    return enTransaccion(app.pool, async (c) => {
      const d = await leerDisenoParaCambiar(c, req.jugadorId);
      const total = totales(d);
      const centralAntes = unidades(d.central);
      const porCasilla = new Map(d.piezas.filter(q => esEscondite(q.tipo)).map(q => [`${q.x},${q.z}`, q]));
      if (r.escondites.length !== porCasilla.size) throw new ErrorJuego('reparto_no_cuadra', 'Faltan escondites en el reparto');
      d.central = r.central;
      for (const e of r.escondites) {
        const q = porCasilla.get(`${e.x},${e.z}`);
        if (!q) throw new ErrorJuego('reparto_no_cuadra', `No hay escondite en (${e.x},${e.z})`);
        q.contenido = e.contenido;
      }
      const error = errorReparto(d, total, centralAntes);
      if (error) throw new ErrorJuego('reparto_no_cuadra', error);
      await guardarDiseno(c, req.jugadorId, d);
      return { diseno: limpiarDiseno(d), jugador: await leerJugador(c, req.jugadorId) };
    });
  });
}
