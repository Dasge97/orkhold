// La web del panel de administración (10-panel-admin.md §2.1): /admin/ y /admin/recursos/<fichero>.
// Ficheros fijos de src/panel; los datos los pide la propia web a las rutas /admin/... con su sesión.
import { readFile } from 'node:fs/promises';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const carpeta = join(dirname(fileURLToPath(import.meta.url)), '..', 'panel');
const TIPOS = { 'panel.js': 'text/javascript; charset=utf-8', 'panel.css': 'text/css; charset=utf-8' };

export default async function (app) {
  app.get('/admin', async (req, reply) => reply.redirect('/admin/'));
  app.get('/admin/', async (req, reply) => {
    reply.header('Cache-Control', 'no-cache').type('text/html; charset=utf-8');
    return readFile(join(carpeta, 'index.html'), 'utf8');
  });
  app.get('/admin/recursos/:fichero', async (req, reply) => {
    const tipo = TIPOS[req.params.fichero];
    if (!tipo) return reply.code(404).send({ error: 'no_existe', mensaje: 'No existe' });
    reply.header('Cache-Control', 'no-cache').type(tipo);
    return readFile(join(carpeta, req.params.fichero), 'utf8');
  });
}
