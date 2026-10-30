// Token de sesión firmado (HMAC-SHA256): "<datos base64url>.<firma>". Dura 30 días (los de admin, 12 h,
// firmados con otra clave derivada para que un token de jugador nunca valga como de admin).
import { createHmac, timingSafeEqual } from 'node:crypto';

const DURACION_MS = 30 * 24 * 3600 * 1000;
export const DURACION_ADMIN_MS = 12 * 3600 * 1000;
export const claveAdmin = secreto => `${secreto}|admin`;

export function firmar(secreto, jugadorId, ahora = Date.now(), duracion = DURACION_MS, extra = {}) {
  const datos = Buffer.from(JSON.stringify({ j: String(jugadorId), e: ahora + duracion, ...extra })).toString('base64url');
  const firma = createHmac('sha256', secreto).update(datos).digest('base64url');
  return `${datos}.${firma}`;
}

/** Devuelve el id del jugador o null si el token no vale. */
export function verificar(secreto, token, ahora = Date.now()) {
  return leerToken(secreto, token, ahora)?.j ?? null;
}

/** Datos del token ({ j, e, ... }) o null si no vale. */
export function leerToken(secreto, token, ahora = Date.now()) {
  if (typeof token !== 'string') return null;
  const [datos, firma] = token.split('.');
  if (!datos || !firma) return null;
  const esperada = createHmac('sha256', secreto).update(datos).digest();
  const recibida = Buffer.from(firma, 'base64url');
  if (recibida.length !== esperada.length || !timingSafeEqual(recibida, esperada)) return null;
  try {
    const d = JSON.parse(Buffer.from(datos, 'base64url').toString('utf8'));
    return d.e > ahora ? d : null;
  } catch {
    return null;
  }
}
