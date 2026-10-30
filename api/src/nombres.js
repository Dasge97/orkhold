// Filtro de nombres de personaje ofensivos (14-cuentas.md §2). Compara sin tildes ni mayúsculas, sin
// espacios ni signos y deshaciendo los números puestos como letras (0→o, 1→i, 3→e, 4→a, 5→s, 7→t).
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const LETRAS = { 0: 'o', 1: 'i', 3: 'e', 4: 'a', 5: 's', 7: 't', '@': 'a', $: 's' };

export function normalizar(texto) {
  return String(texto).toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[013457@$]/g, c => LETRAS[c]).replace(/[^a-zñ]/g, '');
}

const PROHIBIDOS = readFileSync(join(dirname(fileURLToPath(import.meta.url)), 'datos', 'nombres-prohibidos.txt'), 'utf8')
  .split('\n').map(l => l.trim()).filter(l => l && !l.startsWith('#')).map(normalizar).filter(Boolean);

/** El nombre contiene alguna palabra prohibida. */
export function nombreOfensivo(nombre) {
  const n = normalizar(nombre);
  return PROHIBIDOS.some(p => n.includes(p));
}
