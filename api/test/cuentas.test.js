// POST /sesion, GET /jugador, PUT /jugador/personaje, GET /salud.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { preparar, jugador } from './ayudas.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

test('salud responde con la base de datos', async () => {
  const r = await ctx.app.inject({ method: 'GET', url: '/salud' });
  assert.equal(r.statusCode, 200);
  assert.equal(r.json().ok, true);
  assert.equal(r.json().bd, true);
});

test('la sesión crea la cuenta la primera vez y la recupera después', async () => {
  const token_dispositivo = 'dispositivo-de-prueba-0001';
  const a = await ctx.app.inject({ method: 'POST', url: '/sesion', payload: { token_dispositivo } });
  assert.equal(a.statusCode, 200);
  assert.equal(a.json().nuevo, true);
  assert.equal(a.json().jugador.puntuacion, 1000);
  assert.equal(a.json().jugador.nombre, '');
  const b = await ctx.app.inject({ method: 'POST', url: '/sesion', payload: { token_dispositivo } });
  assert.equal(b.json().nuevo, false);
  assert.equal(b.json().jugador.id, a.json().jugador.id);
});

test('token de dispositivo inválido: 400', async () => {
  const r = await ctx.app.inject({ method: 'POST', url: '/sesion', payload: { token_dispositivo: 'corto' } });
  assert.equal(r.statusCode, 400);
  assert.equal(r.json().error, 'peticion_incorrecta');
});

test('sin token o con uno falso: 401', async () => {
  assert.equal((await ctx.app.inject({ method: 'GET', url: '/jugador' })).statusCode, 401);
  const r = await ctx.app.inject({ method: 'GET', url: '/jugador', headers: { authorization: 'Bearer abc.def' } });
  assert.equal(r.statusCode, 401);
});

test('crear personaje: nombre y modelo; nombre repetido da 409', async () => {
  const a = await jugador(ctx.app, null);
  const r = await ctx.app.inject({ method: 'PUT', url: '/jugador/personaje', headers: a.cab, payload: { nombre: 'Gruk', modelo: 'character-female-a' } });
  assert.equal(r.statusCode, 200);
  assert.equal(r.json().jugador.nombre, 'Gruk');
  assert.equal(r.json().jugador.modelo, 'character-female-a');
  const g = await ctx.app.inject({ method: 'GET', url: '/jugador', headers: a.cab });
  assert.equal(g.json().jugador.nombre, 'Gruk');
  const b = await jugador(ctx.app, null);
  const otra = await ctx.app.inject({ method: 'PUT', url: '/jugador/personaje', headers: b.cab, payload: { nombre: 'gruk', modelo: 'character-male-a' } });
  assert.equal(otra.statusCode, 409);
  assert.equal(otra.json().error, 'nombre_ocupado');
});

test('personaje con datos malos: 400', async () => {
  const a = await jugador(ctx.app, null);
  for (const payload of [{ nombre: 'ab', modelo: 'character-male-a' }, { nombre: 'Nombre<script>', modelo: 'character-male-a' }, { nombre: 'Bien', modelo: 'dragon' }]) {
    const r = await ctx.app.inject({ method: 'PUT', url: '/jugador/personaje', headers: a.cab, payload });
    assert.equal(r.statusCode, 400, JSON.stringify(payload));
  }
});

test('una petición sin cuerpo JSON da 4xx, no 500', async () => {
  const a = await jugador(ctx.app, 'SinCuerpo');
  const r = await ctx.app.inject({ method: 'POST', url: '/asalto/buscar', headers: { ...a.cab, 'content-type': 'application/x-www-form-urlencoded' }, payload: 'x=1' });
  assert.equal(r.statusCode, 415);
});
