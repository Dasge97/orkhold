// Construye la aplicación Fastify con todas las rutas (02-arquitectura.md §4).
import Fastify from 'fastify';
import { verificar } from './autenticacion.js';
import { ErrorJuego } from './inventario.js';
import rutasSesion from './rutas/sesion.js';
import rutasJugador from './rutas/jugador.js';
import rutasFuerte from './rutas/fuerte.js';
import rutasBotin from './rutas/botin.js';
import rutasAsaltos from './rutas/asaltos.js';
import rutasPublico from './rutas/publico.js';

export const VERSION = '0.1.0';

/**
 * opciones: { pool, secreto, claveServidor, puebloCliente, registro }
 */
export function crearServidor(opciones) {
  const app = Fastify({ logger: opciones.registro ?? false, bodyLimit: 64 * 1024 });
  app.decorate('pool', opciones.pool);
  app.decorate('opciones', opciones);

  // Autenticación: rutas con { config: { sesion: true } } piden "Authorization: Bearer <token>".
  app.addHook('preHandler', async (req, reply) => {
    if (!req.routeOptions.config?.sesion) return;
    const cab = req.headers.authorization ?? '';
    const id = verificar(opciones.secreto, cab.startsWith('Bearer ') ? cab.slice(7) : '');
    if (!id) return reply.code(401).send({ error: 'sin_sesion', mensaje: 'Sesión no válida o caducada' });
    req.jugadorId = id;
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
  app.register(rutasAsaltos);
  app.register(rutasPublico);
  return app;
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
