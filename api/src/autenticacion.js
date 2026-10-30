// Token de sesión firmado (HMAC-SHA256): "<datos base64url>.<firma>". Dura 30 días.
import { createHmac, timingSafeEqual } from 'node:crypto';

const DURACION_MS = 30 * 24 * 3600 * 1000;

export function firmar(secreto, jugadorId, ahora = Date.now()) {
  const datos = Buffer.from(JSON.stringify({ j: String(jugadorId), e: ahora + DURACION_MS })).toString('base64url');
  const firma = createHmac('sha256', secreto).update(datos).digest('base64url');
  return `${datos}.${firma}`;
}

/** Devuelve el id del jugador o null si el token no vale. */
export function verificar(secreto, token, ahora = Date.now()) {
  if (typeof token !== 'string') return null;
  const [datos, firma] = token.split('.');
  if (!datos || !firma) return null;
  const esperada = createHmac('sha256', secreto).update(datos).digest();
  const recibida = Buffer.from(firma, 'base64url');
  if (recibida.length !== esperada.length || !timingSafeEqual(recibida, esperada)) return null;
  try {
    const { j, e } = JSON.parse(Buffer.from(datos, 'base64url').toString('utf8'));
    return e > ahora ? j : null;
  } catch {
    return null;
  }
}
