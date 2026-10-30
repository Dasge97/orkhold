// Reglas del saqueo que decide el servidor (08-formato-y-pvp.md §4). Funciones sin base de datos:
// - visible(diseno): lo que el atacante puede saber del fuerte (sin contenido, sin enterrados ni trampas).
// - rehacer(diseno, entradas, opciones): repite la exploración con el mapa real y descarta lo imposible.
// - alcanzables(diseno, rotos): casillas con camino libre desde la entrada.
// - resolverMarca(...): qué hay en una casilla marcada y cuánto se lleva el atacante.
// Coordenadas: x = columna de fuerte-base.txt, z = fila desde el sur (las mismas del diseño y del móvil).
import { B } from './balance.js';
import { MAPA, LLEGADA, COFRE, letra, pieza } from './reglas.js';
import { RECURSOS, esEscondite, unidades, vacio } from './escondites.js';

const esMuro = tipo => pieza(tipo)?.categoria === 'muro' || tipo === 'doble_fondo';
const clave = (x, z) => `${x},${z}`;

/** Lo que ve el atacante: muros, puertas y defensores (quietos), cofres y barriles sin contenido. */
export function visible(diseno) {
  const piezas = [];
  for (const q of diseno.piezas ?? []) {
    if (q.tipo === 'trampa' || q.tipo === 'enterrado') continue;
    // El doble fondo parece un muro de piedra.
    piezas.push({ x: q.x, z: q.z, tipo: q.tipo === 'doble_fondo' ? 'muro_piedra' : q.tipo, vida_extra: q.vida_extra ?? 0 });
  }
  return { version: 1, piezas };
}

/** Cuántas piezas y escondites a la vista tiene el fuerte (para "Preparar saqueo"). */
export function resumenVisible(diseno) {
  const v = visible(diseno);
  return { piezas: v.piezas.length, escondites: 1 + v.piezas.filter(q => q.tipo === 'cofre' || q.tipo === 'barril').length };
}

/** Vida de un muro del diseño (lo que hay que golpearlo para romperlo). */
const vidaMuro = q => (q.tipo === 'doble_fondo' ? pieza('muro_piedra').vida : pieza(q.tipo).vida) + (q.vida_extra ?? 0);

/** Mapa de piezas por casilla. */
const porCasilla = diseno => new Map((diseno.piezas ?? []).map(q => [clave(q.x, q.z), q]));

/** Se puede pisar la casilla: dentro del fuerte, sin muro en pie ni cofre o barril. */
function pisable(x, z, piezas, rotos) {
  const l = letra(x, z);
  if (l === 'E') return true;
  if (l !== '.') return false;
  const q = piezas.get(clave(x, z));
  if (!q) return true;
  if (esMuro(q.tipo)) return rotos.has(clave(x, z));
  return !(q.tipo === 'cofre' || q.tipo === 'barril');
}

/** De "desde" a (x, z) en línea recta sin pisar nada que no se pueda pisar (cada cuarto de casilla). */
function tramoLibre(desde, x, z, piezas, rotos) {
  const pasos = Math.max(1, Math.ceil(Math.hypot(x - desde.x, z - desde.z) / 0.25));
  for (let i = 1; i <= pasos; i++) {
    const px = desde.x + (x - desde.x) * i / pasos, pz = desde.z + (z - desde.z) * i / pasos;
    if (!pisable(Math.round(px), Math.round(pz), piezas, rotos)) return false;
  }
  return true;
}

/**
 * Repite la exploración. "entradas": [{ t, x, z, golpe?: { x, z } }] en orden de tiempo (x, z en casillas,
 * con decimales). Devuelve { rotos (Set de "x,z"), descartadas, posiciones: [{ t, x, z }], ultimo }.
 * Una entrada se descarta si va más rápido de lo posible, está dentro de un muro en pie o fuera del fuerte,
 * o golpea algo que no es un muro en pie a su alcance. "danoExtra(t)" da el daño de cada golpe en t (pico).
 */
export function rehacer(diseno, entradas, { duracion, velocidad = B('saqueo.velocidad_max'), danoGolpe = () => 1 } = {}) {
  const piezas = porCasilla(diseno);
  const rotos = new Set();
  const golpes = new Map();
  let descartadas = 0;
  let pos = LLEGADA ? { t: 0, x: LLEGADA.x, z: LLEGADA.z } : { t: 0, x: 1, z: 1 };
  const posiciones = [pos];
  let ultimoGolpe = -10;
  for (const e of entradas) {
    const t = Number(e.t), x = Number(e.x), z = Number(e.z);
    if (![t, x, z].every(Number.isFinite) || t < pos.t || t > duracion + 2) { descartadas++; continue; }
    const dt = Math.max(t - pos.t, 0.05);
    const d = Math.hypot(x - pos.x, z - pos.z);
    if (d > velocidad * dt + 0.5 || !tramoLibre(pos, x, z, piezas, rotos)) { descartadas++; continue; }
    pos = { t, x, z };
    posiciones.push(pos);
    if (e.golpe) {
      const gx = Math.round(Number(e.golpe.x)), gz = Math.round(Number(e.golpe.z));
      const q = piezas.get(clave(gx, gz));
      const cerca = Math.max(Math.abs(gx - x), Math.abs(gz - z)) <= 1.6;
      // Un golpe cada 0,5 s como mucho (el ataque dura 0,6 s).
      if (!q || !esMuro(q.tipo) || rotos.has(clave(gx, gz)) || !cerca || t - ultimoGolpe < 0.5) { descartadas++; continue; }
      ultimoGolpe = t;
      const n = (golpes.get(clave(gx, gz)) ?? 0) + danoGolpe(t);
      golpes.set(clave(gx, gz), n);
      if (n >= vidaMuro(q)) rotos.add(clave(gx, gz));
    }
  }
  return { rotos, descartadas, posiciones, ultimo: pos };
}

/** Posición del atacante en el momento t (la última entrada aceptada antes de t). */
export function posicionEn(posiciones, t) {
  let p = posiciones[0];
  for (const q of posiciones) { if (q.t > t) break; p = q; }
  return p;
}

/** Casillas pisables con camino desde la llegada (búsqueda en anchura por los cuatro lados). */
export function alcanzables(diseno, rotos) {
  const piezas = porCasilla(diseno);
  const vistas = new Set();
  if (!LLEGADA) return vistas;
  const cola = [[LLEGADA.x, LLEGADA.z]];
  vistas.add(clave(LLEGADA.x, LLEGADA.z));
  while (cola.length) {
    const [x, z] = cola.shift();
    for (const [dx, dz] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, nz = z + dz;
      if (vistas.has(clave(nx, nz)) || !pisable(nx, nz, piezas, rotos)) continue;
      vistas.add(clave(nx, nz));
      cola.push([nx, nz]);
    }
  }
  return vistas;
}

/** Una casilla se puede marcar si es pisable y alcanzable, o si tiene al lado (8 casillas) una alcanzable. */
export function marcable(x, z, alcanzadas) {
  if (alcanzadas.has(clave(x, z))) return true;
  for (let dx = -1; dx <= 1; dx++) for (let dz = -1; dz <= 1; dz++)
    if ((dx || dz) && alcanzadas.has(clave(x + dx, z + dz))) return true;
  return false;
}

/** El escondite que hay en (x, z): el cofre central en 'K' o una pieza escondite. null si no hay. */
export function esconditeEn(diseno, x, z) {
  if (COFRE && COFRE.x === x && COFRE.z === z) return { tipo: 'central', contenido: diseno.central ?? vacio(), dueno: diseno, campo: 'central' };
  const q = (diseno.piezas ?? []).find(p => p.x === x && p.z === z && esEscondite(p.tipo));
  return q ? { tipo: q.tipo, contenido: q.contenido ?? vacio(), dueno: q, campo: 'contenido' } : null;
}

/** Lo que se lleva quien acierta: el porcentaje del escondite de cada recurso, redondeado abajo. */
export function botinDe(escondite) {
  const pct = B(`escondite.${escondite.tipo}.porcentaje`);
  const b = vacio();
  for (const r of RECURSOS) b[r] = Math.floor((escondite.contenido[r] ?? 0) * pct / 100);
  return b;
}

/** Respuesta de "mirar" (08 §4.2). "farol": con farol, la tierra dice si hay algo enterrado cerca. */
export function mirar(diseno, x, z, farol) {
  const e = esconditeEn(diseno, x, z);
  if (e && e.tipo !== 'enterrado' && e.tipo !== 'doble_fondo') return unidades(e.contenido) > 0 ? 'Pesa.' : 'Suena hueco.';
  const q = (diseno.piezas ?? []).find(p => p.x === x && p.z === z);
  if (q && esMuro(q.tipo)) {
    if (q.tipo === 'doble_fondo' && farol) return 'Suena hueco.';
    return 'Un muro.';
  }
  if (letra(x, z) !== '.') return 'Nada que mirar.';
  if (!farol) return 'Tierra normal.';
  const radio = B('herramienta.farol.radio');
  const removida = (diseno.piezas ?? []).some(p => p.tipo === 'enterrado' && unidades(p.contenido) > 0
    && Math.abs(p.x - x) <= radio && Math.abs(p.z - z) <= radio);
  return removida ? 'Tierra removida.' : 'Tierra normal.';
}

/** Tramo de una puntuación (temporadas, 08 §1.3). */
export function tramo(puntos) {
  if (puntos >= B('temporada.hierro_orco')) return 'hierro_orco';
  if (puntos >= B('temporada.oro')) return 'oro';
  if (puntos >= B('temporada.plata')) return 'plata';
  return 'bronce';
}

export const DIMENSIONES = { ancho: MAPA.ancho, alto: MAPA.alto };
