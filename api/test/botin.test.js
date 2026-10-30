// POST /mazmorra/resultado, POST /pueblo/botin, POST /inventario/sumar.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, CLAVE } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const mazmorra = (cab, payload) => ctx.app.inject({ method: 'POST', url: '/mazmorra/resultado', headers: cab, payload });
const antiguas = id => ctx.pool.query(`UPDATE mazmorras SET creado = now() - interval '1 hour' WHERE jugador_id = $1`, [id]);

test('el botín de la mazmorra se suma; el jefe da la unidad la primera vez, hueso y hierro una vez al día y monedas después', async () => {
  const a = await jugador(ctx.app, 'Explorador');
  const r1 = await mazmorra(a.cab, { jefe_muerto: true, recursos: { moneda: 20, hierro: 2 } });
  assert.equal(r1.statusCode, 200, r1.body);
  assert.equal(r1.json().primera_vez_jefe, true);
  const inv1 = r1.json().jugador.inventario;
  // 20 monedas y 2 hierro del botín, más 3 hueso y 2 hierro del jefe (primera vez del día).
  assert.equal(inv1.moneda, 20); assert.equal(inv1.hierro, 4); assert.equal(inv1.hueso, 3); assert.equal(inv1.orco_jefe, 1);
  assert.ok(r1.json().jugador.progreso.some(p => p.clave === 'jefe1_muerto'));
  await antiguas(a.id);
  const r2 = await mazmorra(a.cab, { jefe_muerto: true, recursos: { hueso: 1 } });
  assert.equal(r2.json().primera_vez_jefe, false);
  // Segunda vez el mismo día: 10 monedas en vez de hueso y hierro.
  assert.equal(r2.json().jugador.inventario.hueso, 4);
  assert.equal(r2.json().jugador.inventario.moneda, 30);
  assert.equal(r2.json().jugador.inventario.orco_jefe, 1);
});

test('botín imposible para una entrada: 400', async () => {
  const a = await jugador(ctx.app, 'Tramposo');
  const r = await mazmorra(a.cab, { jefe_muerto: false, recursos: { moneda: 500 } });
  assert.equal(r.statusCode, 400);
  assert.equal(r.json().error, 'botin_imposible');
  assert.equal((await mazmorra(a.cab, { jefe_muerto: false, recursos: { madera: 3 } })).statusCode, 400);
});

test('dos entradas seguidas en menos de 20 s: 429', async () => {
  const a = await jugador(ctx.app, 'Rapido');
  assert.equal((await mazmorra(a.cab, { jefe_muerto: false, recursos: { moneda: 1 } })).statusCode, 200);
  assert.equal((await mazmorra(a.cab, { jefe_muerto: false, recursos: { moneda: 1 } })).statusCode, 429);
});

test('botín del pueblo con límite por minuto', async () => {
  const a = await jugador(ctx.app, 'Lenador');
  const p = recursos => ctx.app.inject({ method: 'POST', url: '/pueblo/botin', headers: a.cab, payload: { recursos } });
  const r = await p({ madera: 15, moneda: 3 });
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().jugador.inventario.madera, 15);
  assert.equal((await p({ madera: 6 })).statusCode, 429);
  assert.equal((await p({ madera: 5 })).statusCode, 200);
});

test('botín del pueblo desactivado cuando lo lleva el servidor de mundo', async () => {
  await ctx.cerrar();
  const otro = await preparar({ puebloCliente: false });
  try {
    const a = await jugador(otro.app, 'SinPueblo');
    const r = await otro.app.inject({ method: 'POST', url: '/pueblo/botin', headers: a.cab, payload: { recursos: { madera: 1 } } });
    assert.equal(r.statusCode, 403);
  } finally {
    await otro.cerrar();
    ctx = await preparar();
  }
});

test('inventario/sumar solo con la clave del servidor de mundo', async () => {
  const a = await jugador(ctx.app, 'Mundano');
  const s = (cab, payload) => ctx.app.inject({ method: 'POST', url: '/inventario/sumar', headers: cab, payload });
  assert.equal((await s({}, { jugador: a.id, recurso: 'moneda', cantidad: 2 })).statusCode, 403);
  assert.equal((await s({ 'x-clave-servidor': 'mala' }, { jugador: a.id, recurso: 'moneda', cantidad: 2 })).statusCode, 403);
  const r = await s({ 'x-clave-servidor': CLAVE }, { jugador: a.id, recurso: 'moneda', cantidad: 2, origen: 'orco' });
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().jugador.inventario.moneda, 2);
  assert.equal((await s({ 'x-clave-servidor': CLAVE }, { jugador: '999999', recurso: 'moneda', cantidad: 2 })).statusCode, 404);
});
