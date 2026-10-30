// POST /asalto/buscar, POST /asalto/resultado, GET /asaltos/historial, GET /ranking.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador, dar } from './ayudas.js';
import { puntosGanar, puntosPerder } from '../src/reglas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const buscar = cab => ctx.app.inject({ method: 'POST', url: '/asalto/buscar', headers: cab });
const resultado = (cab, payload) => ctx.app.inject({ method: 'POST', url: '/asalto/resultado', headers: cab, payload });
const envejecer = id => ctx.pool.query(`UPDATE asaltos SET creado = now() - interval '2 minutes' WHERE id = $1`, [id]);

test('Elo: +20 y -15 entre iguales; más premio contra uno mejor', () => {
  assert.equal(puntosGanar(1000, 1000), 20);
  assert.equal(puntosPerder(1000, 1000), -15);
  assert.ok(puntosGanar(1000, 1100) > 20);
  assert.ok(puntosPerder(1000, 1100) > -15);
});

test('sin rivales con fuerte, toca un fuerte del sistema', async () => {
  const a = await jugador(ctx.app, 'Solitario');
  const r = await buscar(a.cab);
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().rival.sistema, true);
  assert.ok(r.json().diseno.piezas.length > 0);
});

test('busca un rival de ±100 puntos con fuerte y gana contra él', async () => {
  const defensor = await jugador(ctx.app, 'Defensor');
  await dar(ctx.pool, defensor.id, { madera: 10 });
  const p = await ctx.app.inject({ method: 'PUT', url: '/fuerte', headers: defensor.cab,
    payload: { diseno: { piezas: [3, 4, 5].map(x => ({ x, z: 5, tipo: 'muro_madera' })) } } });
  assert.equal(p.statusCode, 200, p.body);
  const atacante = await jugador(ctx.app, 'Atacante');
  // Los demás jugadores quedan fuera de rango para que el rival sea el defensor.
  await ctx.pool.query(`UPDATE jugadores SET puntuacion = 500 WHERE nombre NOT IN ('Defensor', 'Atacante')`);
  const b = await buscar(atacante.cab);
  assert.equal(b.json().rival.nombre, 'Defensor');
  assert.equal(b.json().diseno.piezas.length, 3);
  await envejecer(b.json().asalto_id);
  const r = await resultado(atacante.cab, { asalto_id: b.json().asalto_id, ganado: true, segundos: 40, muros_rotos: 1 });
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().puntos_antes, 1000);
  assert.equal(r.json().puntos_despues, 1020);
  assert.equal(r.json().recompensa.moneda, 10);
  const materiales = ['madera', 'piedra', 'hierro', 'hueso'].reduce((s, k) => s + r.json().recompensa[k], 0);
  assert.equal(materiales, 2);
  // El defensor lo ve en su historial y no pierde puntos.
  const h = (await ctx.app.inject({ method: 'GET', url: '/asaltos/historial', headers: defensor.cab })).json();
  assert.equal(h.recibidos.length, 1);
  assert.equal(h.recibidos[0].rival, 'Atacante');
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/jugador', headers: defensor.cab })).json().jugador.puntuacion, 1000);
  const hh = (await ctx.app.inject({ method: 'GET', url: '/asaltos/historial', headers: atacante.cab })).json();
  assert.equal(hh.hechos[0].puntos, 20);
});

test('perder da 3 monedas y quita puntos', async () => {
  const a = await jugador(ctx.app, 'Perdedor');
  const b = await buscar(a.cab);
  const r = await resultado(a.cab, { asalto_id: b.json().asalto_id, ganado: false, segundos: 2, muros_rotos: 0 });
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().recompensa.moneda, 3);
  assert.ok(r.json().puntos_despues < 1000);
});

test('tiempos imposibles: victoria de menos de 5 s o más segundos de los que han pasado', async () => {
  const a = await jugador(ctx.app, 'Veloz');
  const b = (await buscar(a.cab)).json();
  assert.equal((await resultado(a.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 3, muros_rotos: 0 })).statusCode, 400);
  assert.equal((await resultado(a.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 60, muros_rotos: 0 })).json().error, 'tiempo_imposible');
  assert.equal((await resultado(a.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 120, muros_rotos: 0 })).statusCode, 400);
});

test('un asalto solo tiene un resultado y solo lo manda su atacante', async () => {
  const a = await jugador(ctx.app, 'Unico');
  const otro = await jugador(ctx.app, 'Ajeno');
  const b = (await buscar(a.cab)).json();
  await envejecer(b.asalto_id);
  assert.equal((await resultado(otro.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 30, muros_rotos: 0 })).statusCode, 404);
  assert.equal((await resultado(a.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 30, muros_rotos: 0 })).statusCode, 200);
  assert.equal((await resultado(a.cab, { asalto_id: b.asalto_id, ganado: true, segundos: 30, muros_rotos: 0 })).statusCode, 409);
});

test('máximo 10 asaltos al día', async () => {
  const a = await jugador(ctx.app, 'Incansable');
  for (let i = 0; i < 10; i++) assert.equal((await buscar(a.cab)).statusCode, 200);
  const r = await buscar(a.cab);
  assert.equal(r.statusCode, 429);
  assert.equal(r.json().error, 'limite_diario');
});

test('ranking: 50 mejores y tu puesto', async () => {
  const a = await jugador(ctx.app, 'Campeon');
  await ctx.pool.query('UPDATE jugadores SET puntuacion = 5000 WHERE id = $1', [a.id]);
  const r = (await ctx.app.inject({ method: 'GET', url: '/ranking', headers: a.cab })).json();
  assert.equal(r.mejores[0].nombre, 'Campeon');
  assert.equal(r.tu_puesto, 1);
  assert.ok(r.mejores.length <= 50);
  const b = await jugador(ctx.app, 'Novato');
  const rb = (await ctx.app.inject({ method: 'GET', url: '/ranking', headers: b.cab })).json();
  assert.ok(rb.tu_puesto > 1);
});
