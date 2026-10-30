// GET /balance: todos los números del juego para el móvil y el servidor de mundo (10-panel-admin.md §3).
import { todos, versionBalance } from '../balance.js';

export default async function (app) {
  app.get('/balance', async (req, reply) => {
    reply.header('Cache-Control', 'no-cache');
    return { version: versionBalance(), valores: todos() };
  });
}
