// Saqueo (08-formato-y-pvp.md §4): preparar, empezar, mirar, rehacer la exploración y resolver.
// Mapa fuerte-base: llegada en (1,8), cofre central en (8,7).
import { test, before, after, beforeEach } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';
import { cambiarBalance } from '../src/balance.js';
import { comprobarTemporada } from '../src/temporadas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });
// Cada test con sus jugadores: los de antes no pueden salir como rivales.
beforeEach(async () => { await ctx.pool.query(`UPDATE jugadores SET nombre = NULL WHERE nombre IS NOT NULL`); });

const pedir = (method, url, payload, headers) => ctx.app.inject({ method, url, payload, headers });

/** Camino a 3 m/s, una entrada cada 0,25 s, pasando por los puntos dados (casillas). Empieza en la llegada. */
function camino(puntos, t0 = 0.25) {
  const e = [];
  let [x, z] = [1, 8], t = t0;
  for (const [px, pz] of puntos) {
    const d = Math.hypot(px - x, pz - z), pasos = Math.max(1, Math.ceil(d / 0.75));
    for (let i = 1; i <= pasos; i++) { e.push({ t: +t.toFixed(2), x: x + (px - x) * i / pasos, z: z + (pz - z) * i / pasos }); t += 0.25; }
    [x, z] = [px, pz];
  }
  return e;
}

/** Defensor con un cofre lleno, un barril vacío y un enterrado encerrado entre 8 muros de piedra. */
async function defensor(nombre) {
  const d = await jugador(ctx.app, nombre);
  await dar(ctx.pool, d.id, { madera: 6, hierro: 1, piedra: 25, moneda: 20 });
  const muros = [];
  for (let dx = -1; dx <= 1; dx++) for (let dz = -1; dz <= 1; dz++) if (dx || dz) muros.push({ x: 10 + dx, z: 10 + dz, tipo: 'muro_piedra' });
  const r = await pedir('PUT', '/fuerte', { diseno: { piezas: [
    { x: 3, z: 3, tipo: 'cofre' }, { x: 5, z: 5, tipo: 'barril' }, { x: 10, z: 10, tipo: 'enterrado' }, ...muros] } }, d.cab);
  assert.equal(r.statusCode, 200, r.body);
  // Todo lo que queda: 20 monedas y 0 de lo demás (6 madera - 4 - 2, 1 hierro - 1, 25 piedra - 1 - 24).
  const rep = await pedir('PUT', '/fuerte/escondites', { central: {}, escondites: [
    { x: 3, z: 3, contenido: { moneda: 12 } }, { x: 5, z: 5, contenido: {} }, { x: 10, z: 10, contenido: { moneda: 8 } }] }, d.cab);
  assert.equal(rep.statusCode, 200, rep.body);
  return d;
}

async function atacante(nombre, monedas = 20) {
  const a = await jugador(ctx.app, nombre);
  await dar(ctx.pool, a.id, { moneda: monedas });
  return a;
}

test('sin rivales: fuerte de prueba, sin tasa, sin Elo y sin botín real', async () => {
  const a = await atacante('Novato');
  // Con 950 puntos toca el fuerte fácil, cuyo cofre central tiene camino desde la entrada.
  await ctx.pool.query('UPDATE jugadores SET puntuacion = 950 WHERE id = $1', [a.id]);
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  assert.equal(p.rival.prueba, true, JSON.stringify(p));
  assert.equal(p.tasa, 0);
  const e = (await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab)).json();
  assert.equal(e.acciones, 3);
  assert.ok(e.diseno.piezas.every(q => q.tipo !== 'enterrado' && q.tipo !== 'trampa' && !q.contenido), 'El atacante ve demasiado');
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 8, z: 7 }], entradas: camino([[7, 7]]) }, a.cab)).json();
  assert.equal(r.prueba, true);
  assert.equal(r.marcas[0].resultado, 'botin');
  assert.equal(r.botin_prueba.moneda, 9);            // 75 % de 12, redondeado abajo
  assert.equal(r.jugador.inventario.moneda, 20, 'El fuerte de prueba no da botín real');
  assert.equal(r.puntos_despues, r.puntos_antes);
});

test('saqueo con botín: mirar, rehacer, marcas, inalcanzable, Elo, escudo y tasa perdida', async () => {
  const d = await defensor('Escondida');
  const a = await atacante('Ladron');
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  assert.equal(p.rival.nombre, 'Escondida', JSON.stringify(p));
  assert.equal(p.rival.escondites, 3);                 // central, cofre y barril (el enterrado no se ve)
  const e = await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab);
  assert.equal(e.statusCode, 200, e.body);
  assert.equal(e.json().jugador.inventario.moneda, 15, 'No cobró la tasa');
  const mir = async (x, z, t) => (await pedir('POST', '/saqueo/mirar', { saqueo_id: p.saqueo_id, x, z, t }, a.cab)).json().respuesta;
  // Mirar dice la verdad sin enseñar el contenido.
  assert.equal(await mir(3, 3, 2.1), 'Pesa.');
  assert.equal(await mir(5, 5, 4), 'Suena hueco.');
  // Mirar el enterrado desde 2 casillas: responde, pero al rehacer la exploración no cuenta.
  assert.equal(await mir(10, 10, 5.25), 'Tierra normal.');
  await ctx.pool.query(`UPDATE saqueos SET empezado = now() - interval '30 seconds' WHERE id = $1`, [p.saqueo_id]);
  const entradas = camino([[4, 4], [3, 4], [5, 4], [6, 4], [6, 6], [8, 8]]);
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 3, z: 3 }, { x: 5, z: 5 }, { x: 10, z: 10 }], entradas }, a.cab)).json();
  assert.deepEqual(r.marcas.map(m => m.resultado), ['botin', 'vacio', 'inalcanzable'], JSON.stringify(r.marcas));
  assert.equal(r.botin.moneda, 6);                      // 50 % de 12
  assert.equal(r.jugador.inventario.moneda, 21);
  assert.equal(r.puntos_despues - r.puntos_antes, 5);
  assert.equal(r.descartadas, 1, 'Solo la mirada lejana se descarta');
  const def = (await pedir('GET', '/jugador', undefined, d.cab)).json().jugador;
  assert.equal(def.inventario.moneda, 14, 'Al defensor le quitan solo lo encontrado');
  assert.equal(def.puntuacion, 995);
  assert.ok(def.escudo_hasta, 'El defensor saqueado con botín tiene escudo');
  // Su informe: lo marcado y lo encontrado.
  const inf = (await pedir('GET', '/saqueo/informes', undefined, d.cab)).json().informes[0];
  assert.equal(inf.atacante, 'Ladron'); assert.equal(inf.encontrado, 1); assert.equal(inf.visto, false);
  assert.equal(inf.miradas.length, 3);
  const v = await pedir('POST', '/saqueo/informes/visto', {}, d.cab);
  assert.equal(v.statusCode, 200, v.body);
  assert.equal((await pedir('GET', '/saqueo/informes', undefined, d.cab)).json().informes[0].visto, true);
  // Con escudo no sale como rival (el siguiente atacante solo puede tocar a "Ladron", que tiene monedas).
  const otro = await atacante('Segundo');
  for (let i = 0; i < 5; i++)
    assert.notEqual((await pedir('POST', '/saqueo/preparar', {}, otro.cab)).json().rival.nombre, 'Escondida');
});

test('sin encontrar nada: la tasa va al defensor y el defensor gana Elo', async () => {
  const d = await defensor('Lista');
  const a = await atacante('Torpe');
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab);
  await ctx.pool.query(`UPDATE saqueos SET empezado = now() - interval '30 seconds' WHERE id = $1`, [p.saqueo_id]);
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 5, z: 5 }, { x: 6, z: 6 }], entradas: camino([[5, 6]]) }, a.cab)).json();
  assert.deepEqual(r.marcas.map(m => m.resultado), ['vacio', 'nada']);
  assert.equal(r.tasa_al_defensor, 5);
  assert.equal(r.puntos_despues - r.puntos_antes, -10);
  const def = (await pedir('GET', '/jugador', undefined, d.cab)).json().jugador;
  assert.equal(def.inventario.moneda, 25);
  assert.equal(def.puntuacion, 1010);
  assert.equal(def.escudo_hasta, '');
});

test('trampas: teletransporte, atravesar muros y golpes al aire se descartan', async () => {
  await defensor('Murada');
  const a = await atacante('Tramposo');
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab);
  await ctx.pool.query(`UPDATE saqueos SET empezado = now() - interval '60 seconds' WHERE id = $1`, [p.saqueo_id]);
  const entradas = [
    { t: 0.5, x: 10, z: 10 },                          // salto de 9 m en medio segundo
    { t: 1, x: 2, z: 8 },
    { t: 1.5, x: 2, z: 8, golpe: { x: 6, z: 6 } },     // golpe a algo que no es un muro ni está cerca
    ...camino([[9, 8]], 2).map(e => ({ ...e })),
    { t: 9, x: 10, z: 10 },                           // dentro de los muros de piedra
  ];
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 10, z: 10 }], entradas }, a.cab)).json();
  assert.ok(r.descartadas >= 3, `descartadas: ${r.descartadas}`);
  assert.equal(r.marcas[0].resultado, 'inalcanzable');
});

test('romper muros a golpes abre camino; demasiado deprisa no cuenta', async () => {
  await defensor('Rompible');
  const a = await atacante('Picador');
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab);
  await ctx.pool.query(`UPDATE saqueos SET empezado = now() - interval '60 seconds' WHERE id = $1`, [p.saqueo_id]);
  const ir = camino([[8, 10]]);
  let t = ir[ir.length - 1].t + 0.6;
  // 10 golpes al muro de piedra de (9,10) desde (8,10), uno cada 0,6 s; antes, uno demasiado seguido.
  const golpes = [{ t: +(t - 0.3).toFixed(2), x: 8, z: 10, golpe: { x: 9, z: 10 } }];
  for (let i = 0; i < 10; i++) { golpes.push({ t: +t.toFixed(2), x: 8, z: 10, golpe: { x: 9, z: 10 } }); t += 0.6; }
  const entradas = [...ir, ...golpes, { t: +(t + 0.3).toFixed(2), x: 9, z: 10 }];
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 10, z: 10 }], entradas }, a.cab)).json();
  assert.deepEqual(r.rotos, [{ x: 9, z: 10 }], JSON.stringify(r));
  assert.equal(r.marcas[0].resultado, 'botin');
  assert.equal(r.botin.moneda, 4);                      // 50 % de 8
});

test('farol: Ferra lo fabrica y la tierra con algo enterrado dice "Tierra removida."', async () => {
  await defensor('Enterradora');
  const a = await atacante('Farolero');
  assert.equal((await pedir('POST', '/herramientas/fabricar', { id: 'farol' }, a.cab)).statusCode, 409, 'Sin hierro ni hueso no se fabrica');
  await dar(ctx.pool, a.id, { hierro: 2, hueso: 1 });
  const f = await pedir('POST', '/herramientas/fabricar', { id: 'farol' }, a.cab);
  assert.equal(f.statusCode, 200, f.body);
  assert.equal(f.json().jugador.inventario.farol, 1);
  assert.equal((await pedir('POST', '/herramientas/fabricar', { id: 'bomba' }, a.cab)).statusCode, 403, 'La bomba necesita vencer a Grimbra');
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  assert.deepEqual(p.herramientas, [{ id: 'farol', cantidad: 1 }]);
  const e = (await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id, herramientas: ['farol'] }, a.cab)).json();
  assert.equal(e.jugador.inventario.farol, 0);
  const mir = async (x, z) => (await pedir('POST', '/saqueo/mirar', { saqueo_id: p.saqueo_id, x, z, t: 2 }, a.cab)).json().respuesta;
  assert.equal(await mir(10, 12), 'Tierra normal.');
  assert.equal(await mir(10, 10), 'Tierra removida.');
  assert.equal(await mir(10, 11), 'Un muro.');
});

test('saqueos por día: con el balance a 1, el segundo se rechaza', async () => {
  await cambiarBalance(ctx.pool, 'saqueo.saqueos_dia', 1, 'test');
  try {
    const a = await atacante('Ansioso');
    const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
    await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id }, a.cab);
    const r = await pedir('POST', '/saqueo/preparar', {}, a.cab);
    assert.equal(r.statusCode, 429);
  } finally {
    await cambiarBalance(ctx.pool, 'saqueo.saqueos_dia', 10, 'test');
  }
});

test('temporada: al cerrar, estandarte por tramo y puntuación hacia 1000', async () => {
  const a = await jugador(ctx.app, 'Veterana');
  await ctx.pool.query('UPDATE jugadores SET puntuacion = 1300 WHERE id = $1', [a.id]);
  await comprobarTemporada(ctx.pool);
  await ctx.pool.query(`UPDATE temporadas SET fin = now() - interval '1 second' WHERE cerrada IS NULL`);
  const t = await comprobarTemporada(ctx.pool);
  assert.ok(t.numero >= 2);
  const j = (await pedir('GET', '/jugador', undefined, a.cab)).json().jugador;
  assert.equal(j.estandarte, 'hierro_orco');
  assert.equal(j.puntuacion, 1150);
});

test('bomba: rompe los muros de un 3x3 a los 2 s y abre camino; sin bomba elegida no cuenta', async () => {
  await defensor('Bombardeada');
  const a = await atacante('Artificiero');
  await ctx.pool.query(`INSERT INTO progreso (jugador_id, clave, valor) VALUES ($1, 'jefe2_muerto', '1')`, [a.id]);
  await dar(ctx.pool, a.id, { bomba: 1 });
  const p = (await pedir('POST', '/saqueo/preparar', {}, a.cab)).json();
  const e = await pedir('POST', '/saqueo/empezar', { saqueo_id: p.saqueo_id, herramientas: ['bomba'] }, a.cab);
  assert.equal(e.statusCode, 200, e.body);
  await ctx.pool.query(`UPDATE saqueos SET empezado = now() - interval '60 seconds' WHERE id = $1`, [p.saqueo_id]);
  const ir = camino([[8, 10]]);
  const t = ir[ir.length - 1].t;
  const entradas = [...ir, { t: t + 0.25, x: 8, z: 10, bomba: true }, { t: t + 0.5, x: 8, z: 10, bomba: true }, { t: t + 3, x: 8, z: 10 }];
  const r = (await pedir('POST', '/saqueo/resolver', { saqueo_id: p.saqueo_id, marcas: [{ x: 10, z: 10 }], entradas }, a.cab)).json();
  assert.equal(r.rotos.length, 3, JSON.stringify(r.rotos));     // (9,9), (9,10) y (9,11)
  assert.equal(r.marcas[0].resultado, 'botin');
  assert.equal(r.descartadas, 1, 'La segunda bomba no estaba elegida');
});
