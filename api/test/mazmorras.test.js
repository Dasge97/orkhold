// Mazmorras 2, 3 y 4 (11-mazmorras.md): se abren por orden, botín y jefe por mazmorra, y desbloqueos.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const pedir = (method, url, payload, headers) => ctx.app.inject({ method, url, payload, headers });
const mazmorra = (cab, n, jefe_muerto, recursos = {}) => pedir('POST', '/mazmorra/resultado', { mazmorra: n, jefe_muerto, recursos }, cab);
const pasar = id => ctx.pool.query(`UPDATE mazmorras SET creado = now() - interval '1 minute' WHERE jugador_id = $1`, [id]);

test('la mazmorra 2 se abre al vencer a Urzog; su jefa da una bomba y abre pico, doble fondo y muro de hierro', async () => {
  const a = await jugador(ctx.app, 'Aventurera');
  assert.equal((await mazmorra(a.cab, 2, false, { hierro: 5 })).statusCode, 403);
  assert.equal((await mazmorra(a.cab, 1, true)).statusCode, 200);
  await pasar(a.id);
  const r = await mazmorra(a.cab, 2, true, { hierro: 20 });
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().primera_vez_jefe, true);
  const inv = r.json().jugador.inventario;
  assert.equal(inv.bomba, 1);
  assert.equal(inv.hierro, 2 + 20 + 6);              // Urzog (2) + botín (20) + Grimbra (6)
  assert.equal((await mazmorra(a.cab, 2, false, { hierro: 26 })).statusCode, 400, 'Más del máximo de la mazmorra 2');
  // Desbloqueos.
  const h = (await pedir('GET', '/herramientas', undefined, a.cab)).json().herramientas;
  assert.equal(h.find(x => x.id === 'pico').disponible, true);
  assert.equal(h.find(x => x.id === 'sabueso').disponible, false);
  await dar(ctx.pool, a.id, { piedra: 10, madera: 5 });
  const f = await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'muro_hierro' }, { x: 4, z: 3, tipo: 'doble_fondo' }] } }, a.cab);
  assert.equal(f.statusCode, 200, f.body);
});

test('sin vencer a Grimbra: ni muro de hierro ni pico ni mazmorra 3', async () => {
  const b = await jugador(ctx.app, 'Novata');
  await dar(ctx.pool, b.id, { piedra: 10, hierro: 10, madera: 10 });
  assert.equal((await pedir('PUT', '/fuerte', { diseno: { piezas: [{ x: 3, z: 3, tipo: 'muro_hierro' }] } }, b.cab)).statusCode, 403);
  assert.equal((await pedir('POST', '/herramientas/fabricar', { id: 'pico' }, b.cab)).statusCode, 403);
  assert.equal((await mazmorra(b.cab, 3, false)).statusCode, 403);
});
