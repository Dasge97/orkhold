// Inventario y datos del jugador en la base de datos.
// Los recursos (moneda, madera, piedra, hierro, hueso) viven en los escondites del fuerte (escondites.js);
// la tabla inventario guarda sus totales y las unidades que no se esconden (orco jefe, herramientas).
import { TODO, maxSaqueosDia, limpiarDiseno } from './reglas.js';
import { RECURSOS, meter, sacar, totales, ocupacion } from './escondites.js';
import { B } from './balance.js';

export class ErrorJuego extends Error {
  constructor(codigo, mensaje, estado = 400) {
    super(mensaje);
    this.codigo = codigo;
    this.estado = estado;
  }
}

export async function leerInventario(c, jugadorId) {
  const inv = Object.fromEntries(TODO.map(r => [r, 0]));
  for (const f of (await c.query('SELECT recurso, cantidad FROM inventario WHERE jugador_id = $1', [jugadorId])).rows)
    if (f.recurso in inv) inv[f.recurso] = f.cantidad;
  return inv;
}

/** El diseño del fuerte con su contenido, bloqueado para cambiarlo (crea la fila si no existe). */
export async function leerDisenoParaCambiar(c, jugadorId) {
  await c.query(`INSERT INTO fuertes (jugador_id, diseno_json) VALUES ($1, '{"version":1,"piezas":[]}') ON CONFLICT DO NOTHING`, [jugadorId]);
  const f = (await c.query('SELECT diseno_json FROM fuertes WHERE jugador_id = $1 FOR UPDATE', [jugadorId])).rows[0];
  return limpiarDiseno(f.diseno_json);
}

/** Guarda el diseño y pone en la tabla inventario los totales de lo que hay en sus escondites. */
export async function guardarDiseno(c, jugadorId, diseno) {
  await c.query('UPDATE fuertes SET diseno_json = $2, actualizado = now() WHERE jugador_id = $1', [jugadorId, JSON.stringify(limpiarDiseno(diseno))]);
  const t = totales(diseno);
  for (const r of RECURSOS)
    await c.query(
      `INSERT INTO inventario (jugador_id, recurso, cantidad) VALUES ($1, $2, $3)
       ON CONFLICT (jugador_id, recurso) DO UPDATE SET cantidad = EXCLUDED.cantidad`, [jugadorId, r, t[r]]);
}

/**
 * Suma (o resta, con números negativos) a la vez. Los recursos entran en los escondites; lo que no cabe se
 * pierde y se devuelve en "sobrante". Falla con "recursos_insuficientes" si algo queda negativo.
 * Con "forzar", lo que no cabe va al cofre central (pruebas y migraciones).
 */
export async function sumar(c, jugadorId, delta, { forzar = false } = {}) {
  for (const r of Object.keys(delta))
    if (delta[r] && !TODO.includes(r)) throw new ErrorJuego('recurso_desconocido', `Recurso desconocido: ${r}`);
  let sobrante = null;
  if (RECURSOS.some(r => delta[r])) {
    const d = await leerDisenoParaCambiar(c, jugadorId);
    const quitar = Object.fromEntries(RECURSOS.map(r => [r, Math.max(0, -(delta[r] ?? 0))]));
    const faltan = sacar(d, quitar);
    if (faltan.length) throw new ErrorJuego('recursos_insuficientes', `No tienes bastante ${faltan[0]}`, 409);
    sobrante = meter(d, Object.fromEntries(RECURSOS.map(r => [r, Math.max(0, delta[r] ?? 0)])), { forzar }).sobrante;
    await guardarDiseno(c, jugadorId, d);
  }
  for (const [r, n] of Object.entries(delta)) {
    if (!n || RECURSOS.includes(r)) continue;
    if (n > 0) {
      await c.query(
        `INSERT INTO inventario (jugador_id, recurso, cantidad) VALUES ($1, $2, $3)
         ON CONFLICT (jugador_id, recurso) DO UPDATE SET cantidad = inventario.cantidad + EXCLUDED.cantidad`,
        [jugadorId, r, n]);
      continue;
    }
    // Restar: solo si hay bastante (una fila nueva con cantidad negativa no pasaría la comprobación).
    const u = await c.query(
      'UPDATE inventario SET cantidad = cantidad + $3 WHERE jugador_id = $1 AND recurso = $2 AND cantidad + $3 >= 0',
      [jugadorId, r, n]);
    if (u.rowCount !== 1) throw new ErrorJuego('recursos_insuficientes', `No tienes bastante ${r}`, 409);
  }
  const perdido = sobrante ? RECURSOS.reduce((s, r) => s + sobrante[r], 0) : 0;
  return { sobrante, perdido };
}

/** Todo lo que el cliente necesita del jugador. */
export async function leerJugador(c, jugadorId) {
  const j = (await c.query('SELECT id, nombre, modelo, puntuacion, correo, escudo_hasta, estandarte FROM jugadores WHERE id = $1', [jugadorId])).rows[0];
  if (!j) throw new ErrorJuego('jugador_no_existe', 'El jugador no existe', 404);
  const progreso = (await c.query('SELECT clave, valor FROM progreso WHERE jugador_id = $1 ORDER BY clave', [jugadorId])).rows;
  const hoy = (await c.query(
    `SELECT count(*)::int AS n FROM saqueos WHERE atacante_id = $1 AND estado <> 'preparado' AND creado >= $2`,
    [jugadorId, inicioDiaSaqueos()])).rows[0].n;
  const f = (await c.query('SELECT diseno_json FROM fuertes WHERE jugador_id = $1', [jugadorId])).rows[0];
  const o = ocupacion(limpiarDiseno(f?.diseno_json ?? { piezas: [] }));
  return {
    id: String(j.id),
    nombre: j.nombre ?? '',
    modelo: j.modelo ?? '',
    puntuacion: j.puntuacion,
    registrada: !!j.correo,
    estandarte: j.estandarte ?? '',
    escudo_hasta: j.escudo_hasta && j.escudo_hasta > new Date() ? j.escudo_hasta.toISOString() : '',
    inventario: await leerInventario(c, jugadorId),
    capacidad: o.capacidad,
    guardado: o.guardado,
    progreso,
    asaltos_hoy: hoy,
    max_asaltos_dia: maxSaqueosDia(),
  };
}

/** Inicio del "día" de saqueos: la última vez que dieron las saqueo.hora_recarga (UTC). */
export function inicioDiaSaqueos(ahora = new Date()) {
  const h = B('saqueo.hora_recarga');
  const d = new Date(Date.UTC(ahora.getUTCFullYear(), ahora.getUTCMonth(), ahora.getUTCDate(), h));
  if (d > ahora) d.setUTCDate(d.getUTCDate() - 1);
  return d;
}

export async function ponerProgreso(c, jugadorId, clave, valor) {
  await c.query(
    `INSERT INTO progreso (jugador_id, clave, valor) VALUES ($1, $2, $3)
     ON CONFLICT (jugador_id, clave) DO UPDATE SET valor = EXCLUDED.valor`, [jugadorId, clave, valor]);
}
