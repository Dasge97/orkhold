// Escondites (08-formato-y-pvp.md §3): todo lo que se tiene está en el fuerte, con capacidad.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const pedir = (method, url, payload, headers) => ctx.app.inject({ method, url, payload, headers });
const fuerte = async cab => (await pedir('GET', '/fuerte', undefined, cab)).json().diseno;
const mazmorra = (cab, recursos) => pedir('POST', '/mazmorra/resultado', { jefe_muerto: false, recursos }, cab);

test('lo ganado entra en el cofre central; lo que no cabe se pierde y se avisa', async () => {
  const a = await jugador(ctx.app, 'Guardador');
  const r = await mazmorra(a.cab, { moneda: 60 });
  assert.equal(r.statusCode, 200, r.body);
  // El cofre central guarda 50: 10 monedas se pierden.
  assert.equal(r.json().perdido, 10);
  assert.equal(r.json().jugador.inventario.moneda, 50);
  assert.equal(r.json().jugador.capacidad, 50);
  assert.equal(r.json().jugador.guardado, 50);
  assert.equal((await fuerte(a.cab)).central.moneda, 50);
});

test('enterrado sin piedra: 409; cofre con madera y hierro: vale y suma capacidad', async () => {
  const a = await jugador(ctx.app, 'Cavador');
  await dar(ctx.pool, a.id, { madera: 20, hierro: 5 });
  let r = await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 5, z: 5, tipo: 'enterrado' }] } }, a.cab);
  assert.equal(r.statusCode, 409);
  r = await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'cofre' }] } }, a.cab);
  assert.equal(r.statusCode, 200, r.body);
  const j = r.json().jugador;
  assert.equal(j.inventario.madera, 16); assert.equal(j.inventario.hierro, 4);
  assert.equal(j.capacidad, 80);
  // Lo que se gana ahora llena primero el central (50) y luego el cofre.
  await pedir('POST', '/mazmorra/resultado', { jefe_muerto: false, recursos: { moneda: 40 } }, a.cab);
  const d = await fuerte(a.cab);
  assert.equal(d.central.moneda + d.central.madera + d.central.hierro, 50);
  assert.equal(d.piezas[0].contenido.moneda, 10);
});

test('recolocar a mano: tiene que cuadrar y caber', async () => {
  const a = await jugador(ctx.app, 'Recolocador');
  await dar(ctx.pool, a.id, { madera: 10, hierro: 1, moneda: 20 });
  await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'cofre' }, { x: 4, z: 3, tipo: 'barril' }] } }, a.cab);
  await dar(ctx.pool, a.id, { madera: 2 });
  // Ahora: 20 monedas, 6 madera (10 - 4 cofre - 2 barril + 2) en el central.
  const bueno = { central: { moneda: 0, madera: 0 }, escondites: [{ x: 3, z: 3, contenido: { moneda: 20 } }, { x: 4, z: 3, contenido: { madera: 6 } }] };
  let r = await pedir('PUT', '/fuerte/escondites', bueno, a.cab);
  assert.equal(r.statusCode, 200, r.body);
  assert.equal((await fuerte(a.cab)).piezas.find(p => p.tipo === 'barril').contenido.madera, 6);
  // Inventar monedas: no.
  r = await pedir('PUT', '/fuerte/escondites', { central: {}, escondites: [{ x: 3, z: 3, contenido: { moneda: 25 } }, { x: 4, z: 3, contenido: { madera: 6 } }] }, a.cab);
  assert.equal(r.statusCode, 400); assert.match(r.json().mensaje, /no cuadra/);
  // Pasarse de la capacidad del barril (15): no.
  r = await pedir('PUT', '/fuerte/escondites', { central: {}, escondites: [{ x: 3, z: 3, contenido: { moneda: 5 } }, { x: 4, z: 3, contenido: { moneda: 15, madera: 6 } }] }, a.cab);
  assert.equal(r.statusCode, 400); assert.match(r.json().mensaje, /como mucho 15/);
});

test('quitar un escondite: su contenido pasa a los demás; si no cabe, 409', async () => {
  const a = await jugador(ctx.app, 'Mudanza');
  await dar(ctx.pool, a.id, { madera: 8, hierro: 2 });
  await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'cofre' }, { x: 5, z: 3, tipo: 'cofre' }] } }, a.cab);
  // Llena todo: 50 + 30 + 30 = 110 de capacidad; tiene 0 tras pagar. Le damos 100 monedas.
  await pedir('POST', '/mazmorra/resultado', { jefe_muerto: false, recursos: { moneda: 60 } }, a.cab);
  await ctx.pool.query(`UPDATE mazmorras SET creado = now() - interval '1 minute' WHERE jugador_id = $1`, [a.id]);
  await pedir('POST', '/mazmorra/resultado', { jefe_muerto: false, recursos: { moneda: 40 } }, a.cab);
  // Quitar un cofre con 30 dentro: no cabe en el resto (50 + 30 = 80 < 100).
  const r = await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'cofre' }] } }, a.cab);
  assert.equal(r.statusCode, 409);
  assert.equal(r.json().error, 'escondites_llenos');
  assert.equal((await fuerte(a.cab)).piezas.length, 2, 'No debería haber cambiado nada');
});

test('límites de escondites y doble fondo bloqueado', async () => {
  const a = await jugador(ctx.app, 'Barrilero');
  await dar(ctx.pool, a.id, { madera: 100, piedra: 20, hierro: 10 });
  const barriles = Array.from({ length: 7 }, (_, i) => ({ x: 2 + i, z: 2, tipo: 'barril' }));
  assert.equal((await pedir('PUT', '/fuerte', { diseno: { piezas: barriles.slice(0, 6) } }, a.cab)).statusCode, 200);
  const r = await pedir('PUT', '/fuerte', { diseno: { piezas: barriles } }, a.cab);
  assert.equal(r.statusCode, 400); assert.match(r.json().mensaje, /Límite de barril: 6/);
  const d = await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 2, z: 2, tipo: 'doble_fondo' }] } }, a.cab);
  assert.equal(d.statusCode, 403); assert.match(d.json().mensaje, /mazmorra 2/);
});
