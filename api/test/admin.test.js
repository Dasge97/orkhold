// Panel de administración (10-panel-admin.md §2-§3).
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, admin, CLAVE } from './ayudas.js';

let ctx, cab;
before(async () => { ctx = await preparar(); cab = await admin(ctx.app, ctx.pool); });
after(async () => { await ctx.cerrar(); });

const pedir = (method, url, payload, headers = cab) => ctx.app.inject({ method, url, payload, headers });

test('entrar: contraseña mala 401; sin sesión 401; un token de jugador no vale', async () => {
  const mal = await pedir('POST', '/admin/sesion', { correo: 'admin@orkhold.com', contrasena: 'otra' }, {});
  assert.equal(mal.statusCode, 401);
  assert.equal((await pedir('GET', '/admin/resumen', undefined, {})).statusCode, 401);
  const j = await jugador(ctx.app, 'Colado');
  assert.equal((await pedir('GET', '/admin/resumen', undefined, j.cab)).statusCode, 401);
});

test('resumen, balance, historial y deshacer', async () => {
  const r = await pedir('GET', '/admin/resumen');
  assert.equal(r.statusCode, 200, r.body);
  assert.ok(r.json().jugadores >= 1);
  const b = (await pedir('GET', '/admin/balance')).json();
  assert.ok(b.valores.find(v => v.clave === 'saqueo.acciones'));
  assert.equal((await pedir('PUT', '/admin/balance', { cambios: [{ clave: 'saqueo.acciones', valor: 4 }] })).statusCode, 200);
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/balance' })).json().valores['saqueo.acciones'], 4);
  const mal = await pedir('PUT', '/admin/balance', { cambios: [{ clave: 'saqueo.acciones', valor: 50 }] });
  assert.equal(mal.statusCode, 400);
  const h = (await pedir('GET', '/admin/balance/historial')).json().cambios;
  assert.equal(h[0].clave, 'saqueo.acciones'); assert.equal(h[0].quien, 'admin@orkhold.com');
  assert.equal((await pedir('POST', '/admin/balance/deshacer', { id: h[0].id })).statusCode, 200);
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/balance' })).json().valores['saqueo.acciones'], 3);
});

test('jugadores: buscar, ficha, cambiar nombre y bloquear', async () => {
  const j = await jugador(ctx.app, 'Sospechoso');
  const lista = (await pedir('GET', '/admin/jugadores?q=sospe')).json().jugadores;
  assert.equal(lista.length, 1);
  const ficha = await pedir('GET', `/admin/jugadores/${j.id}`);
  assert.equal(ficha.statusCode, 200, ficha.body);
  assert.equal(ficha.json().jugador.nombre, 'Sospechoso');
  assert.equal((await pedir('PUT', `/admin/jugadores/${j.id}/nombre`, { nombre: 'Renombrado' })).statusCode, 200);
  assert.equal((await pedir('POST', `/admin/jugadores/${j.id}/bloquear`, { bloquear: true })).statusCode, 400);
  assert.equal((await pedir('POST', `/admin/jugadores/${j.id}/bloquear`, { bloquear: true, motivo: 'Trampas' })).statusCode, 200);
  const bloqueado = await ctx.app.inject({ method: 'GET', url: '/jugador', headers: j.cab });
  assert.equal(bloqueado.statusCode, 403);
  assert.equal(bloqueado.json().error, 'cuenta_bloqueada');
  await pedir('POST', `/admin/jugadores/${j.id}/bloquear`, { bloquear: false });
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/jugador', headers: j.cab })).json().jugador.nombre, 'Renombrado');
});

test('servidor, avisos y eventos; GET /estado los enseña', async () => {
  assert.equal((await pedir('PUT', '/admin/servidor', { version_minima: 3, mantenimiento: 'Volvemos en 10 minutos' })).statusCode, 200);
  const a = await pedir('POST', '/admin/avisos', { titulo: 'Temporada nueva', texto: 'Empieza la temporada 2' });
  assert.equal(a.statusCode, 200);
  let e = (await ctx.app.inject({ method: 'GET', url: '/estado' })).json();
  assert.equal(e.version_minima, 3); assert.equal(e.mantenimiento, 'Volvemos en 10 minutos');
  assert.equal(e.avisos[0].titulo, 'Temporada nueva');
  await pedir('POST', `/admin/avisos/${a.json().id}/retirar`);
  await pedir('PUT', '/admin/servidor', { version_minima: 0, mantenimiento: '' });
  e = (await ctx.app.inject({ method: 'GET', url: '/estado' })).json();
  assert.equal(e.avisos.length, 0); assert.equal(e.mantenimiento, '');
  const ev = await pedir('POST', '/admin/eventos', { tipo: 'luna_roja', inicio: '2026-11-01T18:00:00Z', fin: '2026-11-01T20:00:00Z', parametros: { botin: 2 } });
  assert.equal(ev.statusCode, 200, ev.body);
  assert.equal((await pedir('GET', '/admin/eventos')).json().eventos[0].tipo, 'luna_roja');
  assert.equal((await pedir('POST', '/admin/eventos', { tipo: 'x', inicio: '2026-11-01T20:00:00Z', fin: '2026-11-01T18:00:00Z' })).statusCode, 400);
});

test('latido del servidor de mundo: con clave cuenta jugadores; sin clave 403', async () => {
  assert.equal((await ctx.app.inject({ method: 'POST', url: '/servidor/latido', payload: { jugadores: 2 } })).statusCode, 403);
  const r = await ctx.app.inject({ method: 'POST', url: '/servidor/latido', payload: { jugadores: 7 }, headers: { 'x-clave-servidor': CLAVE } });
  assert.equal(r.statusCode, 200);
  assert.equal((await pedir('GET', '/admin/resumen')).json().en_pueblo, 7);
});

test('saqueos: lista vacía al principio', async () => {
  const r = await pedir('GET', '/admin/saqueos?sospechosos=1');
  assert.equal(r.statusCode, 200, r.body);
  assert.deepEqual(r.json().saqueos, []);
});
