// Sesiones de jugador (14-cuentas.md §2): el token firmado lleva el id de una fila de "sesiones", que se
// puede anular (cerrar sesión, cambiar o restablecer la contraseña, bloquear la cuenta).
import { firmar } from './autenticacion.js';

const DIAS = 30;

/** Crea una sesión para el jugador y devuelve su token. */
export async function crearSesion(c, secreto, jugadorId) {
  const s = (await c.query(
    `INSERT INTO sesiones (jugador_id, caduca) VALUES ($1, now() + make_interval(days => $2)) RETURNING id`, [jugadorId, DIAS])).rows[0];
  return firmar(secreto, jugadorId, Date.now(), DIAS * 24 * 3600 * 1000, { s: String(s.id) });
}

/** Anula todas las sesiones del jugador menos "salvo" (si se da). */
export async function anularSesiones(c, jugadorId, salvo = null) {
  await c.query(
    `UPDATE sesiones SET anulada = now() WHERE jugador_id = $1 AND anulada IS NULL AND ($2::bigint IS NULL OR id <> $2)`,
    [jugadorId, salvo]);
}
