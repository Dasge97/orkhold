// Arranque: migraciones y servidor en el puerto 3000.
import { crearPool, migrar } from './bd.js';
import { crearServidor } from './servidor.js';

const requerida = n => {
  const v = process.env[n];
  if (!v) { console.error(`Falta la variable de entorno ${n}`); process.exit(1); }
  return v;
};

const pool = crearPool(requerida('BD_URL'));
const aplicadas = await migrar(pool);
if (aplicadas.length) console.log('Migraciones aplicadas:', aplicadas.join(', '));

const app = crearServidor({
  pool,
  secreto: requerida('API_SECRETO'),
  claveServidor: process.env.CLAVE_SERVIDOR ?? '',
  puebloCliente: process.env.PUEBLO_CLIENTE === '1',
  registro: { level: process.env.REGISTRO ?? 'info' },
});

const puerto = Number(process.env.PUERTO ?? 3000);
await app.listen({ host: '0.0.0.0', port: puerto });

for (const s of ['SIGTERM', 'SIGINT'])
  process.on(s, async () => { await app.close(); await pool.end(); process.exit(0); });
