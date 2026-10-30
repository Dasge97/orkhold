// POST /sesion: entra con la cuenta invitada del dispositivo (la crea si no existe) y devuelve el token
// de sesión. Si esa invitada ya se registró, hay que entrar con correo y contraseña (14-cuentas.md §4).
import { z } from 'zod';
import { validar } from '../servidor.js';
import { leerJugador, ErrorJuego } from '../inventario.js';
import { crearSesion } from '../sesiones.js';

const Cuerpo = z.object({ token_dispositivo: z.string().min(16).max(128).regex(/^[A-Za-z0-9_-]+$/) });

export default async function (app) {
  app.post('/sesion', async (req) => {
    const { token_dispositivo } = validar(Cuerpo, req.body);
    const c = await app.pool.connect();
    try {
      const r = await c.query(
        `INSERT INTO jugadores (token_dispositivo) VALUES ($1)
         ON CONFLICT (token_dispositivo) DO UPDATE SET ultimo_acceso = now()
         RETURNING id, (xmax = 0) AS nuevo, correo, bloqueado`, [token_dispositivo]);
      const { id, nuevo, correo, bloqueado } = r.rows[0];
      if (correo) throw new ErrorJuego('cuenta_registrada', 'Esta cuenta tiene correo: entra con tu correo y contraseña', 409);
      if (bloqueado) throw new ErrorJuego('cuenta_bloqueada', 'Esta cuenta está bloqueada', 403);
      return { token: await crearSesion(c, app.opciones.secreto, id), nuevo, jugador: await leerJugador(c, id) };
    } finally {
      c.release();
    }
  });
}
