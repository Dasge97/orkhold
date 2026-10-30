// GET /catalogo, GET/PUT /fuerte: casillas, límites, coste y devolución.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const poner = (cab, piezas) => ctx.app.inject({ method: 'PUT', url: '/fuerte', headers: cab, payload: { diseno: { version: 1, piezas } } });
const muro = (x, z, tipo = 'muro_madera', vida_extra = 0) => ({ x, z, tipo, vida_extra });

test('catálogo con las 7 piezas y los límites del diseño', async () => {
  const r = (await ctx.app.inject({ method: 'GET', url: '/catalogo' })).json();
  assert.equal(r.piezas.length, 7);
  assert.deepEqual(r.limites, { muro: 40, trampa: 6, defensor: 4 });
  assert.equal(r.piezas.find(p => p.id === 'muro_piedra').vida, 10);
});

test('fuerte vacío al empezar', async () => {
  const a = await jugador(ctx.app, 'Vacio');
  const r = await ctx.app.inject({ method: 'GET', url: '/fuerte', headers: a.cab });
  assert.deepEqual(r.json().diseno.piezas, []);
});

test('poner 5 muros descuenta 10 de madera y quitarlos la devuelve', async () => {
  const a = await jugador(ctx.app, 'Constructor');
  await dar(ctx.pool, a.id, { madera: 10 });
  const r = await poner(a.cab, [3, 4, 5, 6, 7].map(x => muro(x, 3)));
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().jugador.inventario.madera, 0);
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/fuerte', headers: a.cab })).json().diseno.piezas.length, 5);
  const q = await poner(a.cab, [muro(3, 3)]);
  assert.equal(q.json().jugador.inventario.madera, 8);
});

test('sin recursos: 409 y no cambia nada', async () => {
  const a = await jugador(ctx.app, 'Pobre');
  await dar(ctx.pool, a.id, { madera: 3 });
  const r = await poner(a.cab, [muro(3, 3), muro(4, 3)]);
  assert.equal(r.statusCode, 409);
  assert.equal(r.json().error, 'recursos_insuficientes');
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/fuerte', headers: a.cab })).json().diseno.piezas.length, 0);
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/jugador', headers: a.cab })).json().jugador.inventario.madera, 3);
});

test('casillas prohibidas: valla, entrada, cofre y fuera del mapa', async () => {
  const a = await jugador(ctx.app, 'Prohibido');
  await dar(ctx.pool, a.id, { madera: 50 });
  for (const [x, z] of [[0, 0], [1, 8], [8, 7], [20, 3], [0, 8]]) {
    const r = await poner(a.cab, [muro(x, z)]);
    assert.equal(r.statusCode, 400, `(${x},${z}) debería estar prohibida`);
    assert.equal(r.json().error, 'diseno_no_valido');
  }
});

test('límites: 6 trampas y 4 defensores', async () => {
  const a = await jugador(ctx.app, 'Limites');
  await dar(ctx.pool, a.id, { hierro: 100, moneda: 100 });
  const trampas = n => Array.from({ length: n }, (_, i) => muro(2 + i, 10, 'trampa'));
  assert.equal((await poner(a.cab, trampas(6))).statusCode, 200);
  assert.equal((await poner(a.cab, trampas(7))).statusCode, 400);
  const orcos = n => Array.from({ length: n }, (_, i) => muro(2 + i, 12, 'orco_defensor'));
  assert.equal((await poner(a.cab, orcos(4))).statusCode, 200);
  assert.equal((await poner(a.cab, orcos(5))).statusCode, 400);
});

test('límite de 40 muros (las puertas cuentan)', async () => {
  const a = await jugador(ctx.app, 'Muralla');
  await dar(ctx.pool, a.id, { madera: 200, hierro: 10 });
  const casillas = [];
  for (let z = 1; z <= 14 && casillas.length < 41; z++) for (let x = 2; x <= 14 && casillas.length < 41; x++) if (!(x === 8 && z === 7)) casillas.push([x, z]);
  const piezas = casillas.map(([x, z], i) => muro(x, z, i === 0 ? 'puerta' : 'muro_madera'));
  assert.equal((await poner(a.cab, piezas.slice(0, 40))).statusCode, 200);
  assert.equal((await poner(a.cab, piezas)).statusCode, 400);
});

test('mejoras: 2 huesos cada una, hasta +3, solo defensores', async () => {
  const a = await jugador(ctx.app, 'Mejoras');
  await dar(ctx.pool, a.id, { moneda: 15, hueso: 6, madera: 2 });
  const r = await poner(a.cab, [muro(4, 4, 'esqueleto_defensor', 3)]);
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().jugador.inventario.hueso, 0);
  assert.equal((await poner(a.cab, [muro(4, 4, 'esqueleto_defensor', 4)])).statusCode, 400);
  assert.equal((await poner(a.cab, [muro(4, 4, 'esqueleto_defensor', 3), muro(6, 6, 'muro_madera', 1)])).statusCode, 400);
});

test('el orco jefe gasta la unidad de botín', async () => {
  const a = await jugador(ctx.app, 'Jefazo');
  assert.equal((await poner(a.cab, [muro(5, 5, 'orco_jefe')])).statusCode, 409);
  await dar(ctx.pool, a.id, { orco_jefe: 1 });
  const r = await poner(a.cab, [muro(5, 5, 'orco_jefe')]);
  assert.equal(r.statusCode, 200);
  assert.equal(r.json().jugador.inventario.orco_jefe, 0);
  const q = await poner(a.cab, []);
  assert.equal(q.json().jugador.inventario.orco_jefe, 1);
});

test('pieza desconocida o casilla repetida: 400', async () => {
  const a = await jugador(ctx.app, 'Raro');
  await dar(ctx.pool, a.id, { madera: 10 });
  assert.equal((await poner(a.cab, [muro(3, 3, 'castillo')])).statusCode, 400);
  assert.equal((await poner(a.cab, [muro(3, 3), muro(3, 3)])).statusCode, 400);
});
