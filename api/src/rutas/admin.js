// Panel de administración (10-panel-admin.md §2 y §3). Todas las rutas, menos la de entrar, piden
// sesión de administrador ({ config: { admin: true } }).
import { z } from 'zod';
import { validar } from '../servidor.js';
import { firmar, claveAdmin, DURACION_ADMIN_MS } from '../autenticacion.js';
import { comprobar } from '../contrasenas.js';
import { cambiarBalance, definicion, B } from '../balance.js';
import { leerInventario, ErrorJuego } from '../inventario.js';
import { leerDiseno } from './fuerte.js';
import { leerAjustes } from './estado.js';
import { NombrePersonaje } from './jugador.js';

const admin = { config: { admin: true } };
const Id = z.object({ id: z.string().regex(/^\d+$/) });

/** Saqueos de los últimos "dias" días y cuántos encontraron algo. */
export async function exitoSaqueos(pool, dias) {
  const f = (await pool.query(
    `SELECT count(*)::int AS total, count(*) FILTER (WHERE encontrado > 0)::int AS con_botin
       FROM saqueos WHERE terminado IS NOT NULL AND NOT prueba AND creado > now() - make_interval(days => $1)`, [dias])).rows[0];
  return { total: f.total, con_botin: f.con_botin, porcentaje: f.total ? Math.round(100 * f.con_botin / f.total) : null };
}

export default async function (app) {
  const pool = app.pool;

  app.post('/admin/sesion', async (req) => {
    const { correo, contrasena } = validar(z.object({ correo: z.string().trim().toLowerCase().email(), contrasena: z.string().min(1).max(200) }), req.body);
    const a = (await pool.query('SELECT id, correo, hash FROM administradores WHERE correo = $1', [correo])).rows[0];
    if (!a || !(await comprobar(contrasena, a.hash))) throw new ErrorJuego('credenciales', 'Correo o contraseña incorrectos', 401);
    await pool.query('UPDATE administradores SET ultimo_acceso = now() WHERE id = $1', [a.id]);
    return { token: firmar(claveAdmin(app.opciones.secreto), a.id, Date.now(), DURACION_ADMIN_MS, { c: a.correo }), correo: a.correo };
  });

  // ---------- resumen ----------

  app.get('/admin/resumen', admin, async () => {
    const j = (await pool.query(
      `SELECT count(*)::int AS total,
              count(*) FILTER (WHERE ultimo_acceso >= date_trunc('day', now()))::int AS hoy,
              count(*) FILTER (WHERE bloqueado IS NOT NULL)::int AS bloqueados
         FROM jugadores`)).rows[0];
    const a = await leerAjustes(pool);
    const pueblo = a.jugadores_pueblo;
    const vivo = pueblo && Date.now() - pueblo.actualizado.getTime() < 120000;
    const hoy = (await pool.query(
      `SELECT count(*)::int AS n FROM saqueos WHERE NOT prueba AND creado >= date_trunc('day', now())`)).rows[0].n;
    const semana = await exitoSaqueos(pool, 7);
    let consejo = '';
    if (semana.porcentaje !== null && semana.total >= 20) {
      if (semana.porcentaje < B('saqueo.objetivo_exito_min')) consejo = 'Sube saqueo.acciones';
      else if (semana.porcentaje > B('saqueo.objetivo_exito_max')) consejo = 'Baja saqueo.acciones';
    }
    return {
      jugadores: j.total, activos_hoy: j.hoy, bloqueados: j.bloqueados,
      en_pueblo: vivo ? Number(pueblo.valor) : null,
      saqueos_hoy: hoy, saqueos_semana: semana, consejo,
      objetivo: { min: B('saqueo.objetivo_exito_min'), max: B('saqueo.objetivo_exito_max') },
    };
  });

  // ---------- balance ----------

  app.get('/admin/balance', admin, async () => {
    const filas = (await pool.query('SELECT * FROM balance ORDER BY grupo, clave')).rows;
    return { valores: filas.map(f => ({ ...f, actualizado: f.actualizado.toISOString() })) };
  });

  app.put('/admin/balance', admin, async (req) => {
    const { cambios } = validar(z.object({ cambios: z.array(z.object({ clave: z.string(), valor: z.number() })).min(1).max(200) }), req.body);
    for (const c of cambios) {
      try { await cambiarBalance(pool, c.clave, c.valor, req.admin.correo); }
      catch (e) { throw new ErrorJuego(e.codigo ?? 'balance', e.message); }
    }
    return { ok: true };
  });

  app.get('/admin/balance/historial', admin, async () => {
    const filas = (await pool.query('SELECT * FROM balance_historial ORDER BY id DESC LIMIT 200')).rows;
    return { cambios: filas.map(f => ({ ...f, id: String(f.id), creado: f.creado.toISOString() })) };
  });

  /** Deshacer un cambio: la clave vuelve al valor anterior de ese cambio. */
  app.post('/admin/balance/deshacer', admin, async (req) => {
    const { id } = validar(z.object({ id: z.string().regex(/^\d+$/) }), req.body);
    const h = (await pool.query('SELECT clave, anterior FROM balance_historial WHERE id = $1', [id])).rows[0];
    if (!h) throw new ErrorJuego('no_existe', 'Ese cambio no existe', 404);
    if (!definicion(h.clave)) throw new ErrorJuego('clave_desconocida', 'Esa clave ya no existe');
    await cambiarBalance(pool, h.clave, h.anterior, req.admin.correo);
    return { ok: true };
  });

  // ---------- jugadores ----------

  app.get('/admin/jugadores', admin, async (req) => {
    const { q } = validar(z.object({ q: z.string().trim().max(100).default('') }), req.query);
    const filas = (await pool.query(
      `SELECT id, nombre, correo, puntuacion, creado, ultimo_acceso, bloqueado FROM jugadores
        WHERE $1 = '' OR lower(coalesce(nombre, '')) LIKE '%' || lower($1) || '%' OR coalesce(correo, '') LIKE '%' || lower($1) || '%'
        ORDER BY ultimo_acceso DESC LIMIT 100`, [q])).rows;
    return { jugadores: filas.map(fichaCorta) };
  });

  app.get('/admin/jugadores/:id', admin, async (req) => {
    const { id } = validar(Id, req.params);
    const f = (await pool.query('SELECT * FROM jugadores WHERE id = $1', [id])).rows[0];
    if (!f) throw new ErrorJuego('jugador_no_existe', 'El jugador no existe', 404);
    const c = await pool.connect();
    try {
      const saqueo = s => ({
        id: String(s.id), rival: s.rival ?? 'Cuenta borrada', encontrado: s.encontrado, botin: s.botin_json,
        puntos: s.puntos_atacante ?? s.puntos_defensor, creado: s.creado.toISOString(), descartadas: s.descartadas,
      });
      const hechos = (await c.query(
        `SELECT s.*, d.nombre AS rival FROM saqueos s LEFT JOIN jugadores d ON d.id = s.defensor_id
          WHERE s.atacante_id = $1 AND s.terminado IS NOT NULL ORDER BY s.id DESC LIMIT 20`, [id])).rows.map(saqueo);
      const recibidos = (await c.query(
        `SELECT s.*, a.nombre AS rival, s.puntos_defensor AS puntos_defensor, NULL AS puntos_atacante FROM saqueos s
           LEFT JOIN jugadores a ON a.id = s.atacante_id
          WHERE s.defensor_id = $1 AND s.terminado IS NOT NULL ORDER BY s.id DESC LIMIT 20`, [id])).rows.map(saqueo);
      return {
        jugador: { ...fichaCorta(f), modelo: f.modelo, bloqueado_motivo: f.bloqueado_motivo, verificado: !!f.verificado },
        inventario: await leerInventario(c, id),
        fuerte: await leerDiseno(c, id),
        saqueos: { hechos, recibidos },
      };
    } finally {
      c.release();
    }
  });

  app.post('/admin/jugadores/:id/bloquear', admin, async (req) => {
    const { id } = validar(Id, req.params);
    const { bloquear, motivo } = validar(z.object({ bloquear: z.boolean(), motivo: z.string().trim().max(300).default('') }), req.body);
    if (bloquear && !motivo) throw new ErrorJuego('falta_motivo', 'Escribe el motivo del bloqueo');
    const r = await pool.query(
      `UPDATE jugadores SET bloqueado = CASE WHEN $2 THEN now() ELSE NULL END, bloqueado_motivo = CASE WHEN $2 THEN $3 ELSE NULL END
        WHERE id = $1`, [id, bloquear, motivo]);
    if (!r.rowCount) throw new ErrorJuego('jugador_no_existe', 'El jugador no existe', 404);
    return { ok: true };
  });

  app.put('/admin/jugadores/:id/nombre', admin, async (req) => {
    const { id } = validar(Id, req.params);
    const { nombre } = validar(z.object({ nombre: NombrePersonaje }), req.body);
    try {
      const r = await pool.query('UPDATE jugadores SET nombre = $2 WHERE id = $1', [id, nombre]);
      if (!r.rowCount) throw new ErrorJuego('jugador_no_existe', 'El jugador no existe', 404);
    } catch (e) {
      if (e.code === '23505') throw new ErrorJuego('nombre_ocupado', 'Ese nombre ya lo tiene otro jugador', 409);
      throw e;
    }
    return { ok: true };
  });

  // ---------- saqueos ----------

  app.get('/admin/saqueos', admin, async (req) => {
    const { sospechosos } = validar(z.object({ sospechosos: z.enum(['0', '1']).default('0') }), req.query);
    const filas = (await pool.query(
      `SELECT s.id, s.creado, s.prueba, s.encontrado, s.marcas, s.descartadas, s.segundos_exploracion, s.botin_json,
              a.nombre AS atacante, d.nombre AS defensor, s.atacante_id,
              -- Aciertos en todas las marcas en sus tres últimos saqueos (incluido este).
              (SELECT bool_and(t.encontrado = t.marcas AND t.marcas > 0) FROM (
                 SELECT encontrado, marcas FROM saqueos x WHERE x.atacante_id = s.atacante_id AND x.terminado IS NOT NULL AND x.id <= s.id
                 ORDER BY x.id DESC LIMIT 3) t HAVING count(*) = 3) AS racha
         FROM saqueos s LEFT JOIN jugadores a ON a.id = s.atacante_id LEFT JOIN jugadores d ON d.id = s.defensor_id
        WHERE s.terminado IS NOT NULL ORDER BY s.id DESC LIMIT 300`)).rows;
    const lista = filas.map(f => {
      const motivos = [];
      if (f.racha) motivos.push('Acierta todas las marcas tres veces seguidas');
      if (f.descartadas > 0) motivos.push(`${f.descartadas} entradas descartadas por el servidor`);
      if (f.encontrado > 0 && f.segundos_exploracion < 10) motivos.push('Botín con menos de 10 s de exploración');
      return {
        id: String(f.id), creado: f.creado.toISOString(), prueba: f.prueba, atacante: f.atacante ?? 'Cuenta borrada',
        defensor: f.prueba ? 'Fuerte de prueba' : (f.defensor ?? 'Cuenta borrada'), encontrado: f.encontrado, marcas: f.marcas,
        botin: f.botin_json, segundos: f.segundos_exploracion, sospechoso: motivos,
      };
    });
    return { saqueos: (sospechosos === '1' ? lista.filter(s => s.sospechoso.length) : lista).slice(0, 100) };
  });

  // ---------- eventos ----------

  app.get('/admin/eventos', admin, async () => {
    const filas = (await pool.query('SELECT * FROM eventos ORDER BY inicio DESC LIMIT 100')).rows;
    return { eventos: filas.map(f => ({ ...f, id: String(f.id), inicio: f.inicio.toISOString(), fin: f.fin.toISOString(), creado: f.creado.toISOString() })) };
  });

  app.post('/admin/eventos', admin, async (req) => {
    const e = validar(z.object({
      tipo: z.string().trim().min(1).max(60),
      inicio: z.string().datetime({ offset: true }),
      fin: z.string().datetime({ offset: true }),
      parametros: z.record(z.any()).default({}),
    }), req.body);
    if (new Date(e.fin) <= new Date(e.inicio)) throw new ErrorJuego('fechas', 'El fin tiene que ser después del inicio');
    const f = (await pool.query(
      'INSERT INTO eventos (tipo, inicio, fin, parametros, creado_por) VALUES ($1, $2, $3, $4, $5) RETURNING id',
      [e.tipo, e.inicio, e.fin, JSON.stringify(e.parametros), req.admin.correo])).rows[0];
    return { id: String(f.id) };
  });

  app.delete('/admin/eventos/:id', admin, async (req) => {
    const { id } = validar(Id, req.params);
    await pool.query('DELETE FROM eventos WHERE id = $1', [id]);
    return { ok: true };
  });

  // ---------- servidor y avisos ----------

  app.get('/admin/servidor', admin, async () => {
    const a = await leerAjustes(pool);
    return {
      version_minima: Number(a.version_minima.valor), mantenimiento: a.mantenimiento.valor,
      jugadores_pueblo: Number(a.jugadores_pueblo.valor), latido: a.jugadores_pueblo.actualizado.toISOString(),
    };
  });

  app.put('/admin/servidor', admin, async (req) => {
    const s = validar(z.object({ version_minima: z.number().int().min(0).max(1000000), mantenimiento: z.string().max(300) }), req.body);
    await pool.query(`UPDATE ajustes_servidor SET valor = $1, actualizado = now() WHERE clave = 'version_minima'`, [String(s.version_minima)]);
    await pool.query(`UPDATE ajustes_servidor SET valor = $1, actualizado = now() WHERE clave = 'mantenimiento'`, [s.mantenimiento.trim()]);
    return { ok: true };
  });

  app.get('/admin/avisos', admin, async () => {
    const filas = (await pool.query('SELECT * FROM avisos ORDER BY id DESC LIMIT 100')).rows;
    return { avisos: filas.map(f => ({ ...f, id: String(f.id), creado: f.creado.toISOString() })) };
  });

  app.post('/admin/avisos', admin, async (req) => {
    const a = validar(z.object({ titulo: z.string().trim().min(1).max(80), texto: z.string().trim().min(1).max(500) }), req.body);
    const f = (await pool.query('INSERT INTO avisos (titulo, texto, creado_por) VALUES ($1, $2, $3) RETURNING id',
      [a.titulo, a.texto, req.admin.correo])).rows[0];
    return { id: String(f.id) };
  });

  app.post('/admin/avisos/:id/retirar', admin, async (req) => {
    const { id } = validar(Id, req.params);
    await pool.query('UPDATE avisos SET activo = false WHERE id = $1', [id]);
    return { ok: true };
  });
}

function fichaCorta(f) {
  return {
    id: String(f.id), nombre: f.nombre ?? '', correo: f.correo ?? '', puntuacion: f.puntuacion,
    creado: f.creado.toISOString(), ultimo_acceso: f.ultimo_acceso.toISOString(), bloqueado: !!f.bloqueado,
  };
}
