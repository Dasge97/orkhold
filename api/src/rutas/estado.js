// GET /estado: lo que el móvil mira al arrancar (10-panel-admin.md §2.2, pestañas Servidor y Avisos):
// versión mínima del cliente, mensaje de mantenimiento y avisos activos.
// POST /servidor/latido: el servidor de mundo dice cuántos jugadores hay en el pueblo (cada 30 s).
import { z } from 'zod';
import { validar, esServidorDeMundo } from '../servidor.js';

export async function leerAjustes(pool) {
  return Object.fromEntries((await pool.query('SELECT clave, valor, actualizado FROM ajustes_servidor')).rows.map(f => [f.clave, f]));
}

export default async function (app) {
  app.get('/estado', async (req, reply) => {
    const a = await leerAjustes(app.pool);
    const avisos = (await app.pool.query(
      `SELECT id, titulo, texto, creado FROM avisos WHERE activo ORDER BY id DESC LIMIT 5`)).rows
      .map(f => ({ id: String(f.id), titulo: f.titulo, texto: f.texto, creado: f.creado.toISOString() }));
    reply.header('Cache-Control', 'no-cache');
    return {
      version_minima: Number(a.version_minima?.valor ?? 0),
      mantenimiento: a.mantenimiento?.valor ?? '',
      avisos,
    };
  });

  app.post('/servidor/latido', async (req, reply) => {
    if (!esServidorDeMundo(app, req)) return reply.code(403).send({ error: 'sin_permiso', mensaje: 'Solo el servidor de mundo' });
    const { jugadores } = validar(z.object({ jugadores: z.number().int().min(0).max(10000) }), req.body);
    await app.pool.query(
      `UPDATE ajustes_servidor SET valor = $1, actualizado = now() WHERE clave = 'jugadores_pueblo'`, [String(jugadores)]);
    return { ok: true };
  });
}
