// GET /publico/ranking: sin sesión, solo nombre y puntuación, para la web orkhold.com.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

test('devuelve los mejores sin sesión y sin identificadores', async () => {
  await jugador(ctx.app, 'Primera');
  const b = await jugador(ctx.app, 'Segunda');
  await jugador(ctx.app, null);   // sin nombre: no sale
  await ctx.pool.query('UPDATE jugadores SET puntuacion = 1200 WHERE id = $1', [b.id]);
  const r = await ctx.app.inject({ method: 'GET', url: '/publico/ranking' });
  assert.equal(r.statusCode, 200, r.body);
  const d = r.json();
  assert.deepEqual(d.mejores.map(m => m.nombre), ['Segunda', 'Primera']);
  assert.deepEqual(Object.keys(d.mejores[0]).sort(), ['nombre', 'puesto', 'puntuacion']);
  assert.equal(d.jugadores, 2);
  assert.equal(d.asaltos, 0);
  assert.ok(d.actualizado);
  assert.match(r.headers['cache-control'], /max-age=60/);
});
