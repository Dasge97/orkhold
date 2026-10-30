// Utilidades de los tests: base de datos de pruebas vacía y migrada, servidor y jugadores.
import { randomBytes } from 'node:crypto';
import { crearPool, migrar } from '../src/bd.js';
import { crearServidor } from '../src/servidor.js';

export const SECRETO = 'secreto-de-pruebas';
export const CLAVE = 'clave-servidor-pruebas';

export async function preparar({ puebloCliente = true } = {}) {
  const url = process.env.BD_URL_PRUEBAS;
  if (!url) throw new Error('Falta BD_URL_PRUEBAS');
  const pool = crearPool(url);
  await pool.query('DROP SCHEMA public CASCADE; CREATE SCHEMA public;');
  await migrar(pool);
  const app = crearServidor({ pool, secreto: SECRETO, claveServidor: CLAVE, puebloCliente });
  await app.ready();
  return { app, pool, cerrar: async () => { await app.close(); await pool.end(); } };
}

/** Crea un jugador con nombre y devuelve { token, id, cab }. */
export async function jugador(app, nombre, modelo = 'character-male-a') {
  const r = await app.inject({ method: 'POST', url: '/sesion', payload: { token_dispositivo: randomBytes(16).toString('hex') } });
  const { token, jugador } = r.json();
  const cab = { authorization: `Bearer ${token}` };
  if (nombre) await app.inject({ method: 'PUT', url: '/jugador/personaje', headers: cab, payload: { nombre, modelo } });
  return { token, id: jugador.id, cab };
}

/** Da recursos a un jugador directamente en la base de datos. */
export async function dar(pool, id, recursos) {
  for (const [r, n] of Object.entries(recursos))
    await pool.query(
      `INSERT INTO inventario (jugador_id, recurso, cantidad) VALUES ($1,$2,$3)
       ON CONFLICT (jugador_id, recurso) DO UPDATE SET cantidad = inventario.cantidad + EXCLUDED.cantidad`, [id, r, n]);
}

/** Crea un administrador y devuelve la cabecera con su sesión. */
export async function admin(app, pool, correo = 'admin@orkhold.com', contrasena = 'contrasena-de-admin') {
  const { cifrar } = await import('../src/contrasenas.js');
  await pool.query('INSERT INTO administradores (correo, hash) VALUES ($1, $2) ON CONFLICT (correo) DO NOTHING', [correo, await cifrar(contrasena)]);
  const r = await app.inject({ method: 'POST', url: '/admin/sesion', payload: { correo, contrasena } });
  return { authorization: `Bearer ${r.json().token}` };
}
