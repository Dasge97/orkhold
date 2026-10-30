// Balance del juego (10-panel-admin.md §1): los números viven en la tabla "balance". Los valores por
// defecto están en datos/balance.json; al arrancar se crean las filas que falten sin pisar las cambiadas.
// La API los lee de memoria con B(clave).
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

export const DEFECTO = JSON.parse(readFileSync(join(dirname(fileURLToPath(import.meta.url)), 'datos', 'balance.json'), 'utf8'));
const porClave = new Map(DEFECTO.map(f => [f.clave, f]));

let valores = new Map(DEFECTO.map(f => [f.clave, f.valor]));
let version = 0;

/** Valor de una clave. Una clave que no existe es un error de programación. */
export function B(clave) {
  const v = valores.get(clave);
  if (v === undefined) throw new Error(`Clave de balance desconocida: ${clave}`);
  return v;
}

/** Valor o "porDefecto" si la clave no existe (para claves que se construyen, como costes opcionales). */
export const Bo = (clave, porDefecto = 0) => valores.get(clave) ?? porDefecto;

export const versionBalance = () => version;
export const todos = () => Object.fromEntries(valores);
export const definicion = clave => porClave.get(clave);

/** Crea las filas que falten y carga la tabla en memoria. */
export async function cargarBalance(pool) {
  for (const f of DEFECTO)
    await pool.query(
      `INSERT INTO balance (clave, valor, por_defecto, grupo, descripcion, minimo, maximo)
       VALUES ($1, $2, $2, $3, $4, $5, $6)
       ON CONFLICT (clave) DO UPDATE SET por_defecto = EXCLUDED.por_defecto, grupo = EXCLUDED.grupo,
         descripcion = EXCLUDED.descripcion, minimo = EXCLUDED.minimo, maximo = EXCLUDED.maximo`,
      [f.clave, f.valor, f.grupo, f.descripcion, f.minimo, f.maximo]);
  await recargarBalance(pool);
}

export async function recargarBalance(pool) {
  const filas = (await pool.query('SELECT clave, valor FROM balance')).rows;
  const v = (await pool.query(`SELECT coalesce((extract(epoch FROM max(actualizado)) * 1000)::bigint, 0) AS v FROM balance`)).rows[0].v;
  valores = new Map(DEFECTO.map(f => [f.clave, f.valor]));
  for (const f of filas) if (porClave.has(f.clave)) valores.set(f.clave, f.valor);
  version = Number(v);
}

/** Cambia una clave (desde el panel). Devuelve la fila nueva o lanza un error con mensaje. */
export async function cambiarBalance(pool, clave, valor, quien) {
  const d = porClave.get(clave);
  if (!d) throw Object.assign(new Error(`Clave desconocida: ${clave}`), { codigo: 'clave_desconocida' });
  if (typeof valor !== 'number' || !Number.isFinite(valor) || valor < d.minimo || valor > d.maximo)
    throw Object.assign(new Error(`${clave}: entre ${d.minimo} y ${d.maximo}`), { codigo: 'fuera_de_rango' });
  const c = await pool.connect();
  try {
    await c.query('BEGIN');
    const antes = (await c.query('SELECT valor FROM balance WHERE clave = $1 FOR UPDATE', [clave])).rows[0];
    if (antes.valor !== valor) {
      await c.query('UPDATE balance SET valor = $2, actualizado = clock_timestamp(), actualizado_por = $3 WHERE clave = $1', [clave, valor, quien]);
      await c.query('INSERT INTO balance_historial (clave, anterior, nuevo, quien) VALUES ($1, $2, $3, $4)', [clave, antes.valor, valor, quien]);
    }
    await c.query('COMMIT');
  } catch (e) {
    await c.query('ROLLBACK');
    throw e;
  } finally {
    c.release();
  }
  await recargarBalance(pool);
}
