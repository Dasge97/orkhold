// Contraseñas con scrypt y sal propia: "scrypt$<sal base64url>$<hash base64url>". Nunca se guardan en claro.
import { scrypt, randomBytes, timingSafeEqual } from 'node:crypto';
import { promisify } from 'node:util';

const scryptP = promisify(scrypt);
const LARGO = 64;

export async function cifrar(contrasena) {
  const sal = randomBytes(16);
  const h = await scryptP(contrasena, sal, LARGO);
  return `scrypt$${sal.toString('base64url')}$${h.toString('base64url')}`;
}

export async function comprobar(contrasena, guardado) {
  const [tipo, sal, hash] = String(guardado ?? '').split('$');
  if (tipo !== 'scrypt' || !sal || !hash) return false;
  const esperado = Buffer.from(hash, 'base64url');
  const h = await scryptP(contrasena, Buffer.from(sal, 'base64url'), esperado.length);
  return timingSafeEqual(h, esperado);
}

/** Contraseña aleatoria legible (para el primer administrador). */
export const generar = () => randomBytes(18).toString('base64url');
