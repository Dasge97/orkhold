// Inventario y datos del jugador en la base de datos.
import { TODO, MAX_ASALTOS_DIA } from './reglas.js';

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

/** Suma (o resta, con números negativos) a la vez. Falla con "recursos_insuficientes" si algo queda negativo. */
export async function sumar(c, jugadorId, delta) {
  for (const [r, n] of Object.entries(delta)) {
    if (!n) continue;
    if (!TODO.includes(r)) throw new ErrorJuego('recurso_desconocido', `Recurso desconocido: ${r}`);
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
}

/** Todo lo que el cliente necesita del jugador. */
export async function leerJugador(c, jugadorId) {
  const j = (await c.query('SELECT id, nombre, modelo, puntuacion FROM jugadores WHERE id = $1', [jugadorId])).rows[0];
  if (!j) throw new ErrorJuego('jugador_no_existe', 'El jugador no existe', 404);
  const progreso = (await c.query('SELECT clave, valor FROM progreso WHERE jugador_id = $1 ORDER BY clave', [jugadorId])).rows;
  const hoy = (await c.query(
    `SELECT count(*)::int AS n FROM asaltos WHERE atacante_id = $1 AND creado >= date_trunc('day', now())`, [jugadorId])).rows[0].n;
  return {
    id: String(j.id),
    nombre: j.nombre ?? '',
    modelo: j.modelo ?? '',
    puntuacion: j.puntuacion,
    inventario: await leerInventario(c, jugadorId),
    progreso,
    asaltos_hoy: hoy,
    max_asaltos_dia: MAX_ASALTOS_DIA,
  };
}

export async function ponerProgreso(c, jugadorId, clave, valor) {
  await c.query(
    `INSERT INTO progreso (jugador_id, clave, valor) VALUES ($1, $2, $3)
     ON CONFLICT (jugador_id, clave) DO UPDATE SET valor = EXCLUDED.valor`, [jugadorId, clave, valor]);
}
