// Construye la aplicación Fastify con todas las rutas (02-arquitectura.md §4).
import Fastify from 'fastify';
import { timingSafeEqual } from 'node:crypto';
import { leerToken, claveAdmin } from './autenticacion.js';
import { configurarCorreo } from './correo.js';
import { ErrorJuego } from './inventario.js';
import rutasSesion from './rutas/sesion.js';
import rutasJugador from './rutas/jugador.js';
import rutasFuerte from './rutas/fuerte.js';
import rutasBotin from './rutas/botin.js';
import rutasSaqueo from './rutas/saqueo.js';
import rutasHerramientas from './rutas/herramientas.js';
import rutasPublico from './rutas/publico.js';
import rutasBalance from './rutas/balance.js';
import rutasEstado from './rutas/estado.js';
import rutasAdmin from './rutas/admin.js';
import rutasPanel from './rutas/panel.js';
import rutasCuenta from './rutas/cuenta.js';
import { cargarBalance } from './balance.js';

export const VERSION = '0.1.0';

/**
 * opciones: { pool, secreto, claveServidor, puebloCliente, registro, smtpUrl, urlPublica }
 */
export function crearServidor(opciones) {
  const app = Fastify({ logger: opciones.registro ?? false, bodyLimit: 64 * 1024 });
  app.decorate('pool', opciones.pool);
  app.decorate('opciones', opciones);
  configurarCorreo(opciones.smtpUrl);
  // El balance se carga (y se crean las filas que falten) antes de atender nada.
  app.addHook('onReady', async () => { await cargarBalance(opciones.pool); });

  // Autenticación: rutas con { config: { sesion: true } } piden "Authorization: Bearer <token>" de jugador;
  // con { config: { admin: true } }, un token de administrador. Un jugador bloqueado o borrado no pasa.
  app.addHook('preHandler', async (req, reply) => {
    const cfg = req.routeOptions.config ?? {};
    if (!cfg.sesion && !cfg.admin) return;
    const cab = req.headers.authorization ?? '';
    const token = cab.startsWith('Bearer ') ? cab.slice(7) : '';
    if (cfg.admin) {
      const d = leerToken(claveAdmin(opciones.secreto), token);
      if (!d) return reply.code(401).send({ error: 'sin_sesion', mensaje: 'Sesión de administrador no válida o caducada' });
      req.admin = { id: d.j, correo: d.c };
      return;
    }
    const d = leerToken(opciones.secreto, token);
    if (!d?.s) return reply.code(401).send({ error: 'sin_sesion', mensaje: 'Sesión no válida o caducada' });
    const j = (await opciones.pool.query(
      `SELECT j.bloqueado, s.anulada, s.caduca < now() AS caducada
         FROM sesiones s JOIN jugadores j ON j.id = s.jugador_id WHERE s.id = $1 AND j.id = $2`, [d.s, d.j])).rows[0];
    if (!j || j.anulada || j.caducada) return reply.code(401).send({ error: 'sin_sesion', mensaje: 'Sesión no válida o caducada' });
    if (j.bloqueado) return reply.code(403).send({ error: 'cuenta_bloqueada', mensaje: 'Esta cuenta está bloqueada' });
    req.jugadorId = d.j;
    req.sesionId = d.s;
  });

  app.setErrorHandler((err, req, reply) => {
    if (err instanceof ErrorJuego) return reply.code(err.estado).send({ error: err.codigo, mensaje: err.message });
    if (err.validation || (err.statusCode >= 400 && err.statusCode < 500))
      return reply.code(err.statusCode ?? 400).send({ error: 'peticion_incorrecta', mensaje: err.message });
    req.log.error(err);
    return reply.code(500).send({ error: 'interno', mensaje: 'Error interno' });
  });

  app.get('/salud', async () => {
    let bd = false;
    try { await opciones.pool.query('SELECT 1'); bd = true; } catch { /* sin base de datos */ }
    const jugadores = bd ? (await opciones.pool.query('SELECT count(*)::int AS n FROM jugadores')).rows[0].n : null;
    return { ok: bd, bd, version: VERSION, jugadores };
  });

  app.register(rutasSesion);
  app.register(rutasJugador);
  app.register(rutasFuerte);
  app.register(rutasBotin);
  app.register(rutasSaqueo);
  app.register(rutasHerramientas);
  app.register(rutasPublico);
  app.register(rutasBalance);
  app.register(rutasEstado);
  app.register(rutasAdmin);
  app.register(rutasPanel);
  app.register(rutasCuenta);
  return app;
}

/** La petición trae la clave del servidor de mundo (cabecera x-clave-servidor). */
export function esServidorDeMundo(app, req) {
  const clave = Buffer.from(String(req.headers['x-clave-servidor'] ?? ''));
  const buena = Buffer.from(app.opciones.claveServidor ?? '');
  return buena.length > 0 && clave.length === buena.length && timingSafeEqual(clave, buena);
}

/** Valida "datos" con un esquema Zod; si no vale, error 400 con el primer problema. */
export function validar(esquema, datos) {
  const r = esquema.safeParse(datos ?? {});
  if (!r.success) {
    const p = r.error.issues[0];
    throw new ErrorJuego('peticion_incorrecta', `${p.path.join('.') || 'cuerpo'}: ${p.message}`);
  }
  return r.data;
}
