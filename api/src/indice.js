// Arranque: migraciones y servidor en el puerto 3000.
import { crearPool, migrar } from './bd.js';
import { crearServidor } from './servidor.js';
import { comprobarTemporada } from './temporadas.js';

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
  // Solo en el Docker local: los tests de Unity eligen rival por nombre al preparar un saqueo.
  pruebas: process.env.PRUEBAS === '1',
  smtpUrl: process.env.SMTP_URL ?? '',
  urlPublica: process.env.URL_PUBLICA ?? 'https://app.orkhold.com',
  registro: { level: process.env.REGISTRO ?? 'info' },
});

const puerto = Number(process.env.PUERTO ?? 3000);
await app.listen({ host: '0.0.0.0', port: puerto });

// Temporadas: se cierran solas (al arrancar, ya con el balance cargado, y cada hora).
await comprobarTemporada(pool);
setInterval(() => comprobarTemporada(pool).catch(e => app.log.error(e, 'Temporada')), 3600 * 1000).unref();

for (const s of ['SIGTERM', 'SIGINT'])
  process.on(s, async () => { await app.close(); await pool.end(); process.exit(0); });
