// Cuentas con registro (14-cuentas.md §4): registrar (o convertir la invitada), entrar, salir, verificar
// el correo, recuperar y cambiar la contraseña, ver y borrar la cuenta. Las páginas de los enlaces del
// correo las sirve la propia API.
import { z } from 'zod';
import { createHash, randomBytes } from 'node:crypto';
import { validar } from '../servidor.js';
import { enTransaccion } from '../bd.js';
import { leerJugador, ErrorJuego } from '../inventario.js';
import { cifrar, comprobar } from '../contrasenas.js';
import { crearSesion, anularSesiones } from '../sesiones.js';
import { mandar } from '../correo.js';
import { leerToken } from '../autenticacion.js';

const Correo = z.string().trim().toLowerCase().email('correo no válido').max(200);
const Contrasena = z.string().min(8, 'mínimo 8 caracteres').max(128, 'máximo 128 caracteres');
const sesion = { config: { sesion: true } };

const FALLOS_MAX = 5;
const MINUTOS_ESPERA = 15;
const HORAS_VERIFICAR = 48;
const HORAS_RESTABLECER = 1;

const hashToken = t => createHash('sha256').update(t).digest('hex');

/** Crea un enlace de un solo uso y devuelve su token (lo que va en el correo). */
async function crearEnlace(c, jugadorId, tipo, horas) {
  const t = randomBytes(24).toString('base64url');
  await c.query(`DELETE FROM enlaces_cuenta WHERE jugador_id = $1 AND tipo = $2 AND usado IS NULL`, [jugadorId, tipo]);
  await c.query(
    `INSERT INTO enlaces_cuenta (token_hash, jugador_id, tipo, caduca) VALUES ($1, $2, $3, now() + make_interval(hours => $4))`,
    [hashToken(t), jugadorId, tipo, horas]);
  return t;
}

/** Usa un enlace: devuelve el id del jugador o null si no vale. */
async function usarEnlace(c, t, tipo) {
  const f = (await c.query(
    `UPDATE enlaces_cuenta SET usado = now() WHERE token_hash = $1 AND tipo = $2 AND usado IS NULL AND caduca > now()
     RETURNING jugador_id`, [hashToken(String(t ?? '')), tipo])).rows[0];
  return f?.jugador_id ?? null;
}

export default async function (app) {
  const url = app.opciones.urlPublica ?? 'https://app.orkhold.com';

  async function mandarVerificacion(c, jugadorId, correo) {
    const t = await crearEnlace(c, jugadorId, 'verificar', HORAS_VERIFICAR);
    await mandar({
      para: correo, asunto: 'Confirma tu correo de Orkhold', registro: app.log,
      texto: `Hola:\n\nPara confirmar este correo en tu cuenta de Orkhold, abre este enlace (vale ${HORAS_VERIFICAR} horas):\n\n${url}/cuenta/verificar?t=${t}\n\nSi no has sido tú, no hagas nada.\n`,
    });
  }

  /** Datos de la cuenta para el juego y la web. */
  async function leerCuenta(c, id) {
    const j = (await c.query('SELECT correo, verificado, creado FROM jugadores WHERE id = $1', [id])).rows[0];
    return { tipo: j.correo ? 'registrada' : 'invitada', correo: j.correo ?? '', verificado: !!j.verificado, creado: j.creado.toISOString() };
  }

  // ---------- registrar ----------

  app.post('/cuenta/registro', async (req) => {
    const { correo, contrasena } = validar(z.object({ correo: Correo, contrasena: Contrasena }), req.body);
    // Con sesión de invitada se convierte esa cuenta; sin sesión se crea una nueva.
    const cab = req.headers.authorization ?? '';
    let invitada = null;
    if (cab.startsWith('Bearer ')) {
      const d = leerToken(app.opciones.secreto, cab.slice(7));
      const ok = d?.s && (await app.pool.query(
        `SELECT 1 FROM sesiones WHERE id = $1 AND jugador_id = $2 AND anulada IS NULL AND caduca > now()`, [d.s, d.j])).rowCount;
      if (!ok) throw new ErrorJuego('sin_sesion', 'Sesión no válida o caducada', 401);
      invitada = d.j;
    }
    const hash = await cifrar(contrasena);
    return enTransaccion(app.pool, async (c) => {
      if ((await c.query('SELECT 1 FROM jugadores WHERE correo = $1', [correo])).rowCount)
        throw new ErrorJuego('correo_ocupado', 'Ya hay una cuenta con ese correo', 409);
      let id = invitada;
      if (id) {
        const j = (await c.query('SELECT correo, bloqueado FROM jugadores WHERE id = $1 FOR UPDATE', [id])).rows[0];
        if (j.bloqueado) throw new ErrorJuego('cuenta_bloqueada', 'Esta cuenta está bloqueada', 403);
        if (j.correo) throw new ErrorJuego('ya_registrada', 'Esta cuenta ya tiene correo', 409);
        await c.query('UPDATE jugadores SET correo = $2, hash = $3 WHERE id = $1', [id, correo, hash]);
      } else {
        id = (await c.query('INSERT INTO jugadores (correo, hash) VALUES ($1, $2) RETURNING id', [correo, hash])).rows[0].id;
      }
      await mandarVerificacion(c, id, correo);
      return { token: await crearSesion(c, app.opciones.secreto, id), cuenta: await leerCuenta(c, id), jugador: await leerJugador(c, id) };
    });
  });

  // ---------- entrar y salir ----------

  app.post('/cuenta/entrar', async (req) => {
    const { correo, contrasena } = validar(z.object({ correo: Correo, contrasena: z.string().min(1).max(128) }), req.body);
    const espera = (await app.pool.query(
      `SELECT ceil(extract(epoch FROM bloqueado_hasta - now()) / 60)::int AS min FROM intentos_entrada WHERE correo = $1 AND bloqueado_hasta > now()`,
      [correo])).rows[0];
    if (espera) throw new ErrorJuego('demasiados_intentos', `Demasiados intentos. Prueba otra vez en ${espera.min} min`, 429);
    const j = (await app.pool.query('SELECT id, hash, bloqueado FROM jugadores WHERE correo = $1', [correo])).rows[0];
    if (!j || !(await comprobar(contrasena, j.hash))) {
      await app.pool.query(
        `INSERT INTO intentos_entrada (correo, fallos) VALUES ($1, 1)
         ON CONFLICT (correo) DO UPDATE SET fallos = intentos_entrada.fallos + 1`, [correo]);
      await app.pool.query(
        `UPDATE intentos_entrada SET fallos = 0, bloqueado_hasta = now() + make_interval(mins => $2) WHERE correo = $1 AND fallos >= $3`,
        [correo, MINUTOS_ESPERA, FALLOS_MAX]);
      throw new ErrorJuego('credenciales', 'Correo o contraseña incorrectos', 401);
    }
    if (j.bloqueado) throw new ErrorJuego('cuenta_bloqueada', 'Esta cuenta está bloqueada', 403);
    await app.pool.query('DELETE FROM intentos_entrada WHERE correo = $1', [correo]);
    const c = await app.pool.connect();
    try {
      await c.query('UPDATE jugadores SET ultimo_acceso = now() WHERE id = $1', [j.id]);
      return { token: await crearSesion(c, app.opciones.secreto, j.id), cuenta: await leerCuenta(c, j.id), jugador: await leerJugador(c, j.id) };
    } finally {
      c.release();
    }
  });

  app.post('/cuenta/salir', sesion, async (req) => {
    await app.pool.query('UPDATE sesiones SET anulada = now() WHERE id = $1', [req.sesionId]);
    return { ok: true };
  });

  app.get('/cuenta', sesion, async (req) => {
    const c = await app.pool.connect();
    try { return { cuenta: await leerCuenta(c, req.jugadorId) }; } finally { c.release(); }
  });

  // ---------- verificar ----------

  app.post('/cuenta/verificar', sesion, async (req) => {
    return enTransaccion(app.pool, async (c) => {
      const j = (await c.query('SELECT correo, verificado FROM jugadores WHERE id = $1', [req.jugadorId])).rows[0];
      if (!j.correo) throw new ErrorJuego('sin_correo', 'Primero guarda tu cuenta con un correo');
      if (j.verificado) return { ok: true, ya_verificado: true };
      await mandarVerificacion(c, req.jugadorId, j.correo);
      return { ok: true };
    });
  });

  app.get('/cuenta/verificar', async (req, reply) => {
    const id = await enTransaccion(app.pool, async (c) => {
      const id = await usarEnlace(c, req.query?.t, 'verificar');
      if (id) await c.query('UPDATE jugadores SET verificado = coalesce(verificado, now()) WHERE id = $1', [id]);
      return id;
    });
    reply.type('text/html; charset=utf-8');
    return id
      ? pagina('Correo confirmado', '<p>Tu correo ya está confirmado. Puedes cerrar esta página y volver al juego.</p>')
      : pagina('Enlace no válido', '<p>Este enlace ya se usó o ha caducado. Desde el juego puedes pedir otro en Menú › Cuenta.</p>');
  });

  // ---------- recuperar y cambiar la contraseña ----------

  app.post('/cuenta/olvide', async (req) => {
    const { correo } = validar(z.object({ correo: Correo }), req.body);
    // Siempre la misma respuesta, haya cuenta o no, para no decir qué correos existen.
    await enTransaccion(app.pool, async (c) => {
      const j = (await c.query('SELECT id, verificado FROM jugadores WHERE correo = $1', [correo])).rows[0];
      if (!j?.verificado) return;
      const t = await crearEnlace(c, j.id, 'restablecer', HORAS_RESTABLECER);
      await mandar({
        para: correo, asunto: 'Contraseña nueva para Orkhold', registro: app.log,
        texto: `Hola:\n\nPara poner una contraseña nueva en tu cuenta de Orkhold, abre este enlace (vale ${HORAS_RESTABLECER} hora):\n\n${url}/cuenta/restablecer?t=${t}\n\nSi no lo has pedido tú, no hagas nada: tu contraseña sigue igual.\n`,
      });
    });
    return { ok: true, mensaje: 'Si ese correo tiene una cuenta confirmada, te hemos mandado un enlace' };
  });

  app.get('/cuenta/restablecer', async (req, reply) => {
    reply.type('text/html; charset=utf-8');
    const t = String(req.query?.t ?? '').replace(/[^A-Za-z0-9_-]/g, '');
    return pagina('Contraseña nueva', `
      <form id="f"><label for="c">Contraseña nueva</label><input id="c" type="password" minlength="8" maxlength="128" required autocomplete="new-password">
      <small>De 8 a 128 caracteres.</small><button>Guardar contraseña</button><p id="m" role="alert"></p></form>
      <script>
        document.getElementById('f').addEventListener('submit', async e => {
          e.preventDefault();
          const m = document.getElementById('m');
          const r = await fetch('/cuenta/restablecer', { method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ t: ${JSON.stringify(t)}, contrasena: document.getElementById('c').value }) });
          const d = await r.json().catch(() => ({}));
          if (r.ok) e.target.innerHTML = '<p>Contraseña cambiada. Ya puedes entrar en el juego con ella.</p>';
          else m.textContent = d.mensaje || 'No se ha podido cambiar. Prueba otra vez.';
        });
      </script>`);
  });

  app.post('/cuenta/restablecer', async (req) => {
    const { t, contrasena } = validar(z.object({ t: z.string().max(100), contrasena: Contrasena }), req.body);
    const hash = await cifrar(contrasena);
    return enTransaccion(app.pool, async (c) => {
      const id = await usarEnlace(c, t, 'restablecer');
      if (!id) throw new ErrorJuego('enlace_no_valido', 'Este enlace ya se usó o ha caducado. Pide otro desde el juego.', 400);
      await c.query('UPDATE jugadores SET hash = $2 WHERE id = $1', [id, hash]);
      await anularSesiones(c, id);
      return { ok: true };
    });
  });

  app.put('/cuenta/contrasena', sesion, async (req) => {
    const { actual, nueva } = validar(z.object({ actual: z.string().min(1).max(128), nueva: Contrasena }), req.body);
    const j = (await app.pool.query('SELECT hash FROM jugadores WHERE id = $1', [req.jugadorId])).rows[0];
    if (!j.hash) throw new ErrorJuego('sin_correo', 'Primero guarda tu cuenta con un correo');
    if (!(await comprobar(actual, j.hash))) throw new ErrorJuego('credenciales', 'La contraseña actual no es esa', 401);
    const hash = await cifrar(nueva);
    return enTransaccion(app.pool, async (c) => {
      await c.query('UPDATE jugadores SET hash = $2 WHERE id = $1', [req.jugadorId, hash]);
      await anularSesiones(c, req.jugadorId, req.sesionId);
      return { ok: true };
    });
  });

  // ---------- borrar ----------

  app.delete('/cuenta', sesion, async (req) => {
    const b = validar(z.object({ contrasena: z.string().max(128).optional(), confirmar: z.string().max(20).optional() }), req.body);
    const j = (await app.pool.query('SELECT hash FROM jugadores WHERE id = $1', [req.jugadorId])).rows[0];
    if (j.hash) {
      if (!b.contrasena || !(await comprobar(b.contrasena, j.hash))) throw new ErrorJuego('credenciales', 'La contraseña no es esa', 401);
    } else if (b.confirmar !== 'BORRAR') {
      throw new ErrorJuego('falta_confirmar', 'Escribe BORRAR para confirmar');
    }
    // Fuerte, inventario, sesiones y progreso se van con el jugador; en los saqueos queda "Cuenta borrada".
    await app.pool.query('DELETE FROM jugadores WHERE id = $1', [req.jugadorId]);
    return { ok: true };
  });
}

const escapar = s => String(s).replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

/** Página sencilla para los enlaces del correo, con los colores del panel. */
function pagina(titulo, cuerpo) {
  return `<!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escapar(titulo)} · Orkhold</title><link rel="icon" href="data:,"><style>
:root { --bg: oklch(0.98 0.006 155); --sup: #fff; --borde: oklch(0.91 0.01 155); --texto: oklch(0.16 0.008 155); --suave: oklch(0.5 0.014 155);
  --prim: oklch(0.44 0.1 155); --acento: oklch(0.48 0.15 25); color-scheme: light; }
@media (prefers-color-scheme: dark) { :root { --bg: oklch(0.16 0.008 155); --sup: oklch(0.21 0.01 155); --borde: oklch(0.3 0.012 155);
  --texto: oklch(0.98 0.006 155); --suave: oklch(0.84 0.012 155); --prim: oklch(0.8 0.08 155); --acento: oklch(0.6 0.17 25); color-scheme: dark; } }
* { box-sizing: border-box; } body { margin: 0; min-height: 100vh; display: grid; place-items: center; padding: 16px; background: var(--bg); color: var(--texto);
  font: 16px/1.5 Inter, ui-sans-serif, system-ui, sans-serif; }
main { background: var(--sup); border: 1px solid var(--borde); border-radius: 16px; padding: 32px; width: min(440px, 100%); display: grid; gap: 16px; }
.marca { font-weight: 600; letter-spacing: .08em; } .marca span { color: var(--acento); } h1 { margin: 0; font-size: 1.5rem; line-height: 1.2; }
form { display: grid; gap: 8px; } label { font-weight: 500; font-size: .875rem; } small { color: var(--suave); font-size: .75rem; }
input { height: 44px; padding: 0 12px; border-radius: 10px; border: 1px solid var(--borde); background: var(--sup); color: var(--texto); font: inherit; }
button { height: 44px; margin-top: 8px; border-radius: 10px; border: 0; background: var(--prim); color: var(--sup); font: inherit; font-weight: 500; cursor: pointer; }
:focus-visible { outline: 2px solid var(--prim); outline-offset: 2px; } #m { color: oklch(0.6 0.19 27); margin: 0; }
</style></head><body><main><p class="marca">ORK<span>HOLD</span></p><h1>${escapar(titulo)}</h1>${cuerpo}</main></body></html>`;
}
