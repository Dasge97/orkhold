// Balance (10-panel-admin.md §1): valores por defecto, GET /balance y que las reglas lo usan.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';
import { cambiarBalance, B } from '../src/balance.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

test('al arrancar se crean todas las filas con su valor por defecto', async () => {
  const n = (await ctx.pool.query('SELECT count(*)::int AS n FROM balance')).rows[0].n;
  assert.ok(n > 90, `solo ${n} filas`);
  const r = (await ctx.app.inject({ method: 'GET', url: '/balance' })).json();
  assert.equal(r.valores['saqueo.acciones'], 3);
  assert.equal(r.valores['pieza.muro_madera.coste.madera'], 2);
});

test('cambiar un valor cambia el coste al guardar el fuerte y deja historial', async () => {
  const a = await jugador(ctx.app, 'Balanceador');
  await dar(ctx.pool, a.id, { madera: 10 });
  const v0 = (await ctx.app.inject({ method: 'GET', url: '/balance' })).json().version;
  await cambiarBalance(ctx.pool, 'pieza.muro_madera.coste.madera', 5, 'test');
  assert.equal(B('pieza.muro_madera.coste.madera'), 5);
  const r = (await ctx.app.inject({ method: 'GET', url: '/balance' })).json();
  assert.notEqual(r.version, v0);
  const cat = (await ctx.app.inject({ method: 'GET', url: '/catalogo' })).json();
  assert.equal(cat.piezas.find(p => p.id === 'muro_madera').costes.madera, 5);
  const f = await ctx.app.inject({ method: 'PUT', url: '/fuerte', headers: a.cab, payload: { diseno: { piezas: [{ x: 2, z: 2, tipo: 'muro_madera' }] } } });
  assert.equal(f.statusCode, 200, f.body);
  assert.equal(f.json().jugador.inventario.madera, 5);
  const h = (await ctx.pool.query(`SELECT * FROM balance_historial WHERE clave = 'pieza.muro_madera.coste.madera'`)).rows;
  assert.equal(h.length, 1); assert.equal(h[0].anterior, 2); assert.equal(h[0].nuevo, 5);
  await cambiarBalance(ctx.pool, 'pieza.muro_madera.coste.madera', 2, 'test');
});

test('fuera de rango o clave desconocida: error', async () => {
  await assert.rejects(cambiarBalance(ctx.pool, 'saqueo.acciones', 99, 'test'), /entre 1 y 10/);
  await assert.rejects(cambiarBalance(ctx.pool, 'no.existe', 1, 'test'), /desconocida/);
});

test('un valor cambiado no se pisa al volver a arrancar', async () => {
  await cambiarBalance(ctx.pool, 'saqueo.tasa', 7, 'test');
  const { cargarBalance } = await import('../src/balance.js');
  await cargarBalance(ctx.pool);
  assert.equal(B('saqueo.tasa'), 7);
  await cambiarBalance(ctx.pool, 'saqueo.tasa', 5, 'test');
});
