// Escondites (08-formato-y-pvp.md §3): todo lo que un jugador posee está escondido en su fuerte.
// El contenido vive en el diseño (fuertes.diseno_json): "central" es el cofre fijo del centro y cada pieza
// cofre, barril, enterrado o doble_fondo lleva su "contenido". La tabla inventario guarda los totales; los
// dos se cambian juntos, siempre por este módulo.
// - Depositar: primero el cofre central y luego los escondites en el orden en que se construyeron.
// - Sacar (gastar): en el mismo orden. (Decisión 100.)
import { B } from './balance.js';

export const RECURSOS = ['moneda', 'madera', 'piedra', 'hierro', 'hueso'];
export const TIPOS_ESCONDITE = ['cofre', 'barril', 'enterrado', 'doble_fondo'];
export const esEscondite = tipo => TIPOS_ESCONDITE.includes(tipo);

export const vacio = () => Object.fromEntries(RECURSOS.map(r => [r, 0]));
export const unidades = contenido => RECURSOS.reduce((s, r) => s + (contenido?.[r] ?? 0), 0);
export const capacidad = tipo => B(`escondite.${tipo}.capacidad`);

/** Contenido limpio: solo recursos, enteros y no negativos. */
export function limpiarContenido(c) {
  const l = vacio();
  for (const r of RECURSOS) l[r] = Math.max(0, Math.floor(Number(c?.[r] ?? 0)) || 0);
  return l;
}

/** Deja "c" con solo recursos enteros y no negativos, sin cambiar de objeto (otros lo tienen apuntado). */
function arreglar(dueno, campo) {
  const c = dueno[campo] && typeof dueno[campo] === 'object' ? dueno[campo] : (dueno[campo] = {});
  for (const k of Object.keys(c)) if (!RECURSOS.includes(k)) delete c[k];
  for (const r of RECURSOS) c[r] = Math.max(0, Math.floor(Number(c[r] ?? 0)) || 0);
  return c;
}

/** Los escondites de un diseño en orden de depósito: el central y luego las piezas escondite. */
export function escondites(diseno) {
  const lista = [{ tipo: 'central', contenido: arreglar(diseno, 'central') }];
  for (const p of diseno.piezas ?? [])
    if (esEscondite(p.tipo)) lista.push({ tipo: p.tipo, contenido: arreglar(p, 'contenido') });
  return lista;
}

/** Total guardado por recurso. */
export function totales(diseno) {
  const t = vacio();
  for (const e of escondites(diseno)) for (const r of RECURSOS) t[r] += e.contenido[r];
  return t;
}

/** Capacidad total y lo que hay guardado (unidades). */
export function ocupacion(diseno) {
  const lista = escondites(diseno);
  return { capacidad: lista.reduce((s, e) => s + capacidad(e.tipo), 0), guardado: lista.reduce((s, e) => s + unidades(e.contenido), 0) };
}

/**
 * Mete "delta" (solo positivos) en los escondites. Lo que no cabe queda en "sobrante".
 * Con "forzar", lo que no cabe va al cofre central aunque se pase de su capacidad (migración y pruebas).
 */
export function meter(diseno, delta, { forzar = false } = {}) {
  const lista = escondites(diseno);
  const sobrante = vacio(), metido = vacio();
  for (const r of RECURSOS) {
    let n = Math.max(0, delta[r] ?? 0);
    for (const e of lista) {
      if (!n) break;
      const hueco = Math.max(0, capacidad(e.tipo) - unidades(e.contenido));
      const m = Math.min(hueco, n);
      e.contenido[r] += m; metido[r] += m; n -= m;
    }
    if (n && forzar) { lista[0].contenido[r] += n; metido[r] += n; n = 0; }
    sobrante[r] = n;
  }
  return { metido, sobrante };
}

/** Saca "delta" (cantidades positivas a quitar). Devuelve los recursos que faltan (vacío si llegó). */
export function sacar(diseno, delta) {
  const lista = escondites(diseno);
  const t = totales(diseno);
  const faltan = RECURSOS.filter(r => (delta[r] ?? 0) > t[r]);
  if (faltan.length) return faltan;
  for (const r of RECURSOS) {
    let n = Math.max(0, delta[r] ?? 0);
    for (const e of lista) {
      if (!n) break;
      const m = Math.min(e.contenido[r], n);
      e.contenido[r] -= m; n -= m;
    }
  }
  return [];
}

const NOMBRE = { central: 'El cofre central', cofre: 'Un cofre', barril: 'Un barril', enterrado: 'Un enterrado', doble_fondo: 'Un doble fondo' };

/**
 * Error de un reparto hecho a mano: capacidades y que no se cree ni se pierda nada. null si vale.
 * "centralAntes": lo que ya tenía el cofre central (si venía de antes por encima de su capacidad, puede quedarse así, pero no crecer).
 */
export function errorReparto(diseno, totalesEsperados, centralAntes = 0) {
  for (const e of escondites(diseno)) {
    const tope = e.tipo === 'central' ? Math.max(capacidad('central'), centralAntes) : capacidad(e.tipo);
    if (unidades(e.contenido) > tope) return `${NOMBRE[e.tipo]} guarda como mucho ${tope}`;
  }
  const t = totales(diseno);
  for (const r of RECURSOS) if (t[r] !== totalesEsperados[r]) return `El reparto no cuadra: ${r} ${t[r]} de ${totalesEsperados[r]}`;
  return null;
}
