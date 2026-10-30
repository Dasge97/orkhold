// Crea o cambia la contraseña de un administrador del panel: node src/crear-admin.js <correo>
// La contraseña llega por la entrada estándar (nunca como argumento) y no se imprime.
import { crearPool, migrar } from './bd.js';
import { cifrar } from './contrasenas.js';

const correo = (process.argv[2] ?? '').trim().toLowerCase();
if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(correo)) { console.error('Uso: node src/crear-admin.js <correo> < contraseña'); process.exit(1); }
let contrasena = '';
for await (const trozo of process.stdin) contrasena += trozo;
contrasena = contrasena.trim();
if (contrasena.length < 12) { console.error('La contraseña tiene que llegar por la entrada estándar y tener 12 caracteres o más'); process.exit(1); }

const pool = crearPool(process.env.BD_URL);
await migrar(pool);
await pool.query(
  `INSERT INTO administradores (correo, hash) VALUES ($1, $2) ON CONFLICT (correo) DO UPDATE SET hash = EXCLUDED.hash`,
  [correo, await cifrar(contrasena)]);
await pool.end();
console.log(`Administrador listo: ${correo}`);
