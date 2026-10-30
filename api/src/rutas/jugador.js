// GET /jugador y PUT /jugador/personaje.
import { z } from 'zod';
import { validar } from '../servidor.js';
import { leerJugador, ErrorJuego } from '../inventario.js';

export const MODELOS = ['character-male-a', 'character-female-a', 'character-male-c', 'character-female-c'];

const Personaje = z.object({
  nombre: z.string().trim().min(3, 'mínimo 3 letras').max(16, 'máximo 16 letras').regex(/^[\p{L}\p{N} _-]+$/u, 'solo letras, números, espacio, _ y -'),
  modelo: z.enum(MODELOS),
});

export default async function (app) {
  app.get('/jugador', { config: { sesion: true } }, async (req) => {
    const c = await app.pool.connect();
    try { return { jugador: await leerJugador(c, req.jugadorId) }; } finally { c.release(); }
  });

  app.put('/jugador/personaje', { config: { sesion: true } }, async (req) => {
    const { nombre, modelo } = validar(Personaje, req.body);
    const c = await app.pool.connect();
    try {
      try {
        await c.query('UPDATE jugadores SET nombre = $2, modelo = $3 WHERE id = $1', [req.jugadorId, nombre, modelo]);
      } catch (e) {
        if (e.code === '23505') throw new ErrorJuego('nombre_ocupado', 'Ese nombre ya lo tiene otro jugador', 409);
        throw e;
      }
      return { jugador: await leerJugador(c, req.jugadorId) };
    } finally {
      c.release();
    }
  });
}
