// Temporadas (08-formato-y-pvp.md §1.3): duran temporada.semanas. Al cerrarse, cada jugador se lleva el
// estandarte de su tramo (bronce, plata, oro, hierro orco) y su puntuación se acerca a 1000
// (conserva temporada.compresion % de la distancia). Se comprueba al arrancar y cada hora.
import { B } from './balance.js';
import { tramo } from './saqueo.js';

/** Cierra la temporada si ha terminado y abre la siguiente. Devuelve la temporada en curso. */
export async function comprobarTemporada(pool, ahora = new Date()) {
  const c = await pool.connect();
  try {
    await c.query('BEGIN');
    await c.query('LOCK TABLE temporadas IN EXCLUSIVE MODE');
    let t = (await c.query('SELECT * FROM temporadas ORDER BY numero DESC LIMIT 1')).rows[0];
    const semanas = ms => ms + B('temporada.semanas') * 7 * 24 * 3600 * 1000;
    if (!t) {
      t = (await c.query('INSERT INTO temporadas (numero, inicio, fin) VALUES (1, $1, $2) RETURNING *',
        [ahora, new Date(semanas(ahora.getTime()))])).rows[0];
    } else if (!t.cerrada && t.fin <= ahora) {
      const jugadores = (await c.query('SELECT id, puntuacion FROM jugadores WHERE nombre IS NOT NULL')).rows;
      const conserva = B('temporada.compresion') / 100;
      for (const j of jugadores)
        await c.query('UPDATE jugadores SET estandarte = $2, puntuacion = $3 WHERE id = $1',
          [j.id, tramo(j.puntuacion), Math.round(1000 + (j.puntuacion - 1000) * conserva)]);
      await c.query('UPDATE temporadas SET cerrada = $2 WHERE numero = $1', [t.numero, ahora]);
      t = (await c.query('INSERT INTO temporadas (numero, inicio, fin) VALUES ($1, $2, $3) RETURNING *',
        [t.numero + 1, ahora, new Date(semanas(ahora.getTime()))])).rows[0];
    }
    await c.query('COMMIT');
    return t;
  } catch (e) {
    await c.query('ROLLBACK');
    throw e;
  } finally {
    c.release();
  }
}
