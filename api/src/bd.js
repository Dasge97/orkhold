// Conexión a PostgreSQL y migraciones numeradas (migraciones/NNN_nombre.sql), aplicadas al arrancar.
import pg from 'pg';
import { readdir, readFile } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const carpeta = join(dirname(fileURLToPath(import.meta.url)), '..', 'migraciones');

export function crearPool(url) {
  return new pg.Pool({ connectionString: url, max: 10 });
}

export async function migrar(pool) {
  await pool.query(`CREATE TABLE IF NOT EXISTS migraciones (nombre TEXT PRIMARY KEY, aplicada TIMESTAMPTZ NOT NULL DEFAULT now())`);
  const hechas = new Set((await pool.query('SELECT nombre FROM migraciones')).rows.map(r => r.nombre));
  const ficheros = (await readdir(carpeta)).filter(f => /^\d{3}_.+\.sql$/.test(f)).sort();
  const aplicadas = [];
  for (const f of ficheros) {
    if (hechas.has(f)) continue;
    const sql = await readFile(join(carpeta, f), 'utf8');
    const c = await pool.connect();
    try {
      await c.query('BEGIN');
      await c.query(sql);
      await c.query('INSERT INTO migraciones (nombre) VALUES ($1)', [f]);
      await c.query('COMMIT');
      aplicadas.push(f);
    } catch (e) {
      await c.query('ROLLBACK');
      throw new Error(`Migración ${f}: ${e.message}`);
    } finally {
      c.release();
    }
  }
  return aplicadas;
}

/** Ejecuta "trabajo(cliente)" en una transacción. */
export async function enTransaccion(pool, trabajo) {
  const c = await pool.connect();
  try {
    await c.query('BEGIN');
    const r = await trabajo(c);
    await c.query('COMMIT');
    return r;
  } catch (e) {
    await c.query('ROLLBACK');
    throw e;
  } finally {
    c.release();
  }
}
