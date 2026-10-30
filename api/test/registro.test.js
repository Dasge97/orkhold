// Cuentas con registro (14-cuentas.md): convertir la invitada, entrar en otro móvil, verificar, recuperar,
// cambiar la contraseña, intentos, nombres prohibidos, bloqueo y borrar.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { randomBytes } from 'node:crypto';
import { preparar, jugador, dar, admin } from './ayudas.js';
import { enviados } from '../src/correo.js';
import { nombreOfensivo } from '../src/nombres.js';

let ctx;
before(async () => { ctx = await preparar(); });
after(async () => { await ctx.cerrar(); });

const pedir = (method, url, payload, headers = {}) => ctx.app.inject({ method, url, payload, headers });
const cab = token => ({ authorization: `Bearer ${token}` });
const enlace = (para, ruta) => {
  const m = [...enviados].reverse().find(e => e.para === para && e.texto.includes(ruta));
  assert.ok(m, `no se mandó correo a ${para} con ${ruta}`);
  return new URL(m.texto.match(/https?:\/\/\S+/)[0]).searchParams.get('t');
};

test('la invitada se convierte en registrada sin perder nada y entra desde otro móvil', async () => {
  const dispositivo = randomBytes(16).toString('hex');
  const s = (await pedir('POST', '/sesion', { token_dispositivo: dispositivo })).json();
  await pedir('PUT', '/jugador/personaje', { nombre: 'Colona', modelo: 'character-female-a' }, cab(s.token));
  await dar(ctx.pool, s.jugador.id, { moneda: 33 });
  const r = await pedir('POST', '/cuenta/registro', { correo: 'Colona@Ejemplo.com', contrasena: 'una-clave-larga' }, cab(s.token));
  assert.equal(r.statusCode, 200, r.body);
  assert.equal(r.json().jugador.id, s.jugador.id);
  assert.equal(r.json().cuenta.correo, 'colona@ejemplo.com');
  assert.equal(r.json().cuenta.verificado, false);
  // El mismo dispositivo ya no entra como invitada.
  assert.equal((await pedir('POST', '/sesion', { token_dispositivo: dispositivo })).json().error, 'cuenta_registrada');
  // Otro móvil entra con correo y contraseña y ve lo mismo.
  const e = await pedir('POST', '/cuenta/entrar', { correo: 'colona@ejemplo.com', contrasena: 'una-clave-larga' });
  assert.equal(e.statusCode, 200, e.body);
  assert.equal(e.json().jugador.nombre, 'Colona');
  assert.equal(e.json().jugador.inventario.moneda, 33);
  // Correo repetido.
  assert.equal((await pedir('POST', '/cuenta/registro', { correo: 'colona@ejemplo.com', contrasena: 'otra-clave-larga' })).statusCode, 409);
});

test('registro sin sesión crea una cuenta nueva; verificar con el enlace del correo', async () => {
  const r = await pedir('POST', '/cuenta/registro', { correo: 'nueva@ejemplo.com', contrasena: 'clave-de-prueba' });
  assert.equal(r.statusCode, 200, r.body);
  const t = enlace('nueva@ejemplo.com', '/cuenta/verificar');
  const p = await pedir('GET', `/cuenta/verificar?t=${t}`);
  assert.match(p.body, /Correo confirmado/);
  assert.match((await pedir('GET', `/cuenta/verificar?t=${t}`)).body, /Enlace no válido/);
  assert.equal((await pedir('GET', '/cuenta', undefined, cab(r.json().token))).json().cuenta.verificado, true);
  assert.equal((await pedir('POST', '/cuenta/registro', { correo: 'mal', contrasena: 'clave-de-prueba' })).statusCode, 400);
  assert.equal((await pedir('POST', '/cuenta/registro', { correo: 'corta@ejemplo.com', contrasena: '1234' })).statusCode, 400);
});

test('recuperar la contraseña: enlace, contraseña nueva y las sesiones viejas dejan de valer', async () => {
  const r = (await pedir('POST', '/cuenta/registro', { correo: 'olvido@ejemplo.com', contrasena: 'clave-vieja-1' })).json();
  await pedir('GET', `/cuenta/verificar?t=${enlace('olvido@ejemplo.com', '/cuenta/verificar')}`);
  const o = await pedir('POST', '/cuenta/olvide', { correo: 'olvido@ejemplo.com' });
  assert.equal(o.statusCode, 200);
  // Un correo sin cuenta responde lo mismo.
  assert.equal((await pedir('POST', '/cuenta/olvide', { correo: 'nadie@ejemplo.com' })).statusCode, 200);
  const t = enlace('olvido@ejemplo.com', '/cuenta/restablecer');
  assert.match((await pedir('GET', `/cuenta/restablecer?t=${t}`)).body, /Contraseña nueva/);
  assert.equal((await pedir('POST', '/cuenta/restablecer', { t, contrasena: 'clave-nueva-2' })).statusCode, 200);
  assert.equal((await pedir('POST', '/cuenta/restablecer', { t, contrasena: 'clave-nueva-3' })).statusCode, 400);
  assert.equal((await pedir('GET', '/jugador', undefined, cab(r.token))).statusCode, 401);
  assert.equal((await pedir('POST', '/cuenta/entrar', { correo: 'olvido@ejemplo.com', contrasena: 'clave-nueva-2' })).statusCode, 200);
});

test('cambiar la contraseña anula las demás sesiones; salir anula la actual', async () => {
  const a = (await pedir('POST', '/cuenta/registro', { correo: 'cambio@ejemplo.com', contrasena: 'primera-clave' })).json();
  const b = (await pedir('POST', '/cuenta/entrar', { correo: 'cambio@ejemplo.com', contrasena: 'primera-clave' })).json();
  assert.equal((await pedir('PUT', '/cuenta/contrasena', { actual: 'mala-clave', nueva: 'segunda-clave' }, cab(a.token))).statusCode, 401);
  assert.equal((await pedir('PUT', '/cuenta/contrasena', { actual: 'primera-clave', nueva: 'segunda-clave' }, cab(a.token))).statusCode, 200);
  assert.equal((await pedir('GET', '/jugador', undefined, cab(a.token))).statusCode, 200);
  assert.equal((await pedir('GET', '/jugador', undefined, cab(b.token))).statusCode, 401);
  assert.equal((await pedir('POST', '/cuenta/salir', undefined, cab(a.token))).statusCode, 200);
  assert.equal((await pedir('GET', '/jugador', undefined, cab(a.token))).statusCode, 401);
});

test('5 entradas fallidas: 15 minutos sin poder probar ese correo', async () => {
  await pedir('POST', '/cuenta/registro', { correo: 'insistente@ejemplo.com', contrasena: 'la-buena-clave' });
  for (let i = 0; i < 5; i++)
    assert.equal((await pedir('POST', '/cuenta/entrar', { correo: 'insistente@ejemplo.com', contrasena: 'mala' })).statusCode, 401);
  const r = await pedir('POST', '/cuenta/entrar', { correo: 'insistente@ejemplo.com', contrasena: 'la-buena-clave' });
  assert.equal(r.statusCode, 429);
  assert.match(r.json().mensaje, /15 min/);
});

test('nombres ofensivos: no se aceptan, aunque vayan con números o tildes', async () => {
  assert.equal(nombreOfensivo('P3nd3jo'), true);
  assert.equal(nombreOfensivo('Gílipollas'), true);
  assert.equal(nombreOfensivo('Maela'), false);
  assert.equal(nombreOfensivo('Pedro'), false);
  const j = await jugador(ctx.app, null);
  const r = await pedir('PUT', '/jugador/personaje', { nombre: 'Xx_Puta_xX', modelo: 'character-male-a' }, j.cab);
  assert.equal(r.statusCode, 400);
  assert.match(r.json().mensaje, /no está permitido/);
});

test('bloqueo: ni entrar ni seguir con la sesión abierta', async () => {
  const r = (await pedir('POST', '/cuenta/registro', { correo: 'malo@ejemplo.com', contrasena: 'clave-del-malo' })).json();
  const cabAdmin = await admin(ctx.app, ctx.pool);
  await pedir('POST', `/admin/jugadores/${r.jugador.id}/bloquear`, { bloquear: true, motivo: 'Trampas' }, cabAdmin);
  assert.equal((await pedir('GET', '/jugador', undefined, cab(r.token))).statusCode, 403);
  assert.equal((await pedir('POST', '/cuenta/entrar', { correo: 'malo@ejemplo.com', contrasena: 'clave-del-malo' })).statusCode, 403);
});

test('borrar la cuenta: con contraseña, o escribiendo BORRAR si es invitada', async () => {
  const r = (await pedir('POST', '/cuenta/registro', { correo: 'adios@ejemplo.com', contrasena: 'clave-de-adios' })).json();
  assert.equal((await pedir('DELETE', '/cuenta', { contrasena: 'otra' }, cab(r.token))).statusCode, 401);
  assert.equal((await pedir('DELETE', '/cuenta', { contrasena: 'clave-de-adios' }, cab(r.token))).statusCode, 200);
  assert.equal((await pedir('POST', '/cuenta/entrar', { correo: 'adios@ejemplo.com', contrasena: 'clave-de-adios' })).statusCode, 401);
  const inv = await jugador(ctx.app, 'Invitado');
  assert.equal((await pedir('DELETE', '/cuenta', {}, inv.cab)).statusCode, 400);
  assert.equal((await pedir('DELETE', '/cuenta', { confirmar: 'BORRAR' }, inv.cab)).statusCode, 200);
  assert.equal((await pedir('GET', '/jugador', undefined, inv.cab)).statusCode, 401);
});
