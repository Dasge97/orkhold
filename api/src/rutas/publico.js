// Rutas públicas sin sesión, para la web orkhold.com. Solo datos que ya son públicos dentro del juego
// (nombre de personaje y puntuación). Nunca identificadores de jugador ni datos de cuenta.
export default async function rutasPublico(app) {
  app.get('/publico/ranking', async (req, reply) => {
    const mejores = (await app.pool.query(
      `SELECT nombre, puntuacion FROM jugadores WHERE nombre IS NOT NULL ORDER BY puntuacion DESC, id ASC LIMIT 50`)).rows
      .map((f, i) => ({ puesto: i + 1, nombre: f.nombre, puntuacion: f.puntuacion }));
    const jugadores = (await app.pool.query(`SELECT count(*)::int AS n FROM jugadores WHERE nombre IS NOT NULL`)).rows[0].n;
    const saqueos = (await app.pool.query(`SELECT count(*)::int AS n FROM saqueos WHERE estado = 'terminado' AND NOT prueba`)).rows[0].n;
    reply.header('Cache-Control', 'public, max-age=60');
    // "asaltos" se queda para la web de orkhold.com, que ya lo lee: ahora cuenta saqueos.
    return { mejores, jugadores, saqueos, asaltos: saqueos, actualizado: new Date().toISOString() };
  });
}
