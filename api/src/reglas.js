// Reglas del juego que la API hace cumplir. Son las mismas que ReglasFuerte.cs en el cliente:
// casillas construibles de fuerte-base.txt, límites por categoría, costes y mejoras con hueso.
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const datos = join(dirname(fileURLToPath(import.meta.url)), 'datos');

export const CATALOGO = JSON.parse(readFileSync(join(datos, 'catalogo.json'), 'utf8'));
export const RECURSOS = ['moneda', 'madera', 'piedra', 'hierro', 'hueso'];
export const UNIDADES = ['orco_jefe'];
export const TODO = [...RECURSOS, ...UNIDADES];
export const MATERIALES = ['madera', 'piedra', 'hierro', 'hueso'];
export const MAX_ASALTOS_DIA = 10;

const piezasPorId = new Map(CATALOGO.piezas.map(p => [p.id, p]));
export const pieza = id => piezasPorId.get(id);

// ---------- mapa fuerte-base ----------

function leerMapa(texto) {
  const lineas = texto.replace(/\r/g, '').split('\n');
  const i = lineas.findIndex(l => l.trim() === '---');
  const filas = lineas.slice(i + 1).map(l => l.trimEnd()).filter(l => l.length > 0);
  const ancho = Math.max(...filas.map(f => f.length));
  return { filas: filas.map(f => f.padEnd(ancho, '.')), ancho, alto: filas.length };
}

export const MAPA = leerMapa(readFileSync(join(datos, 'fuerte-base.txt'), 'utf8'));

/** Letra de la casilla (x = columna, z = fila contada desde el sur). */
export function letra(x, z) {
  if (x < 0 || z < 0 || x >= MAPA.ancho || z >= MAPA.alto) return ' ';
  return MAPA.filas[MAPA.alto - 1 - z][x];
}

function buscar(l) {
  for (let f = 0; f < MAPA.alto; f++)
    for (let c = 0; c < MAPA.ancho; c++)
      if (MAPA.filas[f][c] === l) return { c, f };
  return null;
}

/** Casilla junto a la entrada 'E', hacia dentro: ahí aparece el atacante y no se construye. */
export const LLEGADA = (() => {
  const e = buscar('E');
  if (!e) return null;
  for (const [dc, df] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
    const c = e.c + dc, f = e.f + df;
    if (MAPA.filas[f]?.[c] === '.') return { x: c, z: MAPA.alto - 1 - f };
  }
  return null;
})();

export const COFRE = (() => { const k = buscar('K'); return k ? { x: k.c, z: MAPA.alto - 1 - k.f } : null; })();

export function construible(x, z) {
  if (letra(x, z) !== '.') return false;
  return !(LLEGADA && LLEGADA.x === x && LLEGADA.z === z);
}

// ---------- diseño ----------

/** Comprueba casillas, tipos, mejoras y límites. Devuelve un mensaje de error o null. */
export function errorDiseno(diseno) {
  if (!diseno || !Array.isArray(diseno.piezas)) return 'Diseño sin piezas';
  const vistas = new Set();
  const cuenta = { muro: 0, trampa: 0, defensor: 0 };
  for (const q of diseno.piezas) {
    const p = pieza(q.tipo);
    if (!p) return `Pieza desconocida: ${q.tipo}`;
    if (!Number.isInteger(q.x) || !Number.isInteger(q.z) || !construible(q.x, q.z)) return `No se puede construir en (${q.x},${q.z})`;
    const clave = `${q.x},${q.z}`;
    if (vistas.has(clave)) return `Casilla repetida (${clave})`;
    vistas.add(clave);
    const extra = q.vida_extra ?? 0;
    if (!Number.isInteger(extra) || extra < 0 || extra > CATALOGO.mejora.maxima) return `Mejora fuera de rango en (${clave})`;
    if (extra > 0 && p.categoria !== 'defensor') return `Solo se mejoran defensores (${clave})`;
    cuenta[p.categoria]++;
  }
  for (const k of Object.keys(cuenta))
    if (cuenta[k] > CATALOGO.limites[k]) return `Límite de ${k}: ${CATALOGO.limites[k]}`;
  return null;
}

/** Lo que cuesta un diseño entero: recursos de cada pieza, huesos de las mejoras y unidades de botín. */
export function coste(diseno) {
  const t = Object.fromEntries(TODO.map(r => [r, 0]));
  for (const q of diseno?.piezas ?? []) {
    const p = pieza(q.tipo);
    if (!p) continue;
    for (const [r, n] of Object.entries(p.costes)) t[r] += n;
    if (p.requiere_unidad) t[p.requiere_unidad] += 1;
    t.hueso += (q.vida_extra ?? 0) * CATALOGO.mejora.huesos;
  }
  return t;
}

/** Diseño limpio (solo los campos que se guardan). */
export function limpiarDiseno(d) {
  return { version: 1, piezas: (d?.piezas ?? []).map(q => ({ x: q.x, z: q.z, tipo: q.tipo, vida_extra: q.vida_extra ?? 0 })) };
}

// ---------- asaltos ----------

/** Probabilidad de ganar del atacante (Elo). */
export const esperado = (ra, rb) => 1 / (1 + 10 ** ((rb - ra) / 400));
/** Ganar: +20 entre iguales, más contra uno mejor. Perder: -15 entre iguales, menos contra uno mejor. */
export const puntosGanar = (ra, rb) => Math.round(40 * (1 - esperado(ra, rb)));
export const puntosPerder = (ra, rb) => -Math.round(30 * esperado(ra, rb));

/** Fuerte del sistema según la puntuación del atacante, con su puntuación de referencia. */
export function fuerteSistema(puntuacion) {
  const nombre = puntuacion < 1000 ? 'facil' : puntuacion < 1100 ? 'medio' : 'dificil';
  const puntos = { facil: 900, medio: 1000, dificil: 1100 }[nombre];
  const diseno = JSON.parse(readFileSync(join(datos, 'fuertes-sistema', nombre + '.json'), 'utf8'));
  return { nombre, puntos, diseno: limpiarDiseno(diseno) };
}

/** Recompensa: ganar 10 monedas + 2 materiales al azar; perder 3 monedas. */
export function recompensa(ganado, azar = Math.random) {
  const r = Object.fromEntries(RECURSOS.map(k => [k, 0]));
  if (ganado) {
    r.moneda = 10;
    for (let i = 0; i < 2; i++) r[MATERIALES[Math.floor(azar() * MATERIALES.length)]]++;
  } else r.moneda = 3;
  return r;
}

// ---------- límites de botín ----------

/** Máximo por entrada a la mazmorra (catacumbas: ~8 enemigos, 6 monedas sueltas y el jefe). */
export const MAX_MAZMORRA = { moneda: 60, madera: 0, piedra: 0, hierro: 10, hueso: 12 };
/** Máximo de botín del pueblo por minuto (provisional hasta el servidor de mundo). */
export const MAX_PUEBLO_MINUTO = { moneda: 30, madera: 20, piedra: 20, hierro: 10, hueso: 10 };
