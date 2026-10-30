// POST /sesion: crea o recupera la cuenta por token de dispositivo y devuelve el token de sesión.
import { z } from 'zod';
import { firmar } from '../autenticacion.js';
import { validar } from '../servidor.js';
import { leerJugador } from '../inventario.js';

const Cuerpo = z.object({ token_dispositivo: z.string().min(16).max(128).regex(/^[A-Za-z0-9_-]+$/) });

export default async function (app) {
  app.post('/sesion', async (req) => {
    const { token_dispositivo } = validar(Cuerpo, req.body);
    const c = await app.pool.connect();
    try {
      const r = await c.query(
        `INSERT INTO jugadores (token_dispositivo) VALUES ($1)
         ON CONFLICT (token_dispositivo) DO UPDATE SET ultimo_acceso = now()
         RETURNING id, (xmax = 0) AS nuevo`, [token_dispositivo]);
      const { id, nuevo } = r.rows[0];
      return { token: firmar(app.opciones.secreto, id), nuevo, jugador: await leerJugador(c, id) };
    } finally {
      c.release();
    }
  });
}
