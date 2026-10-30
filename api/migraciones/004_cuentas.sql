-- Cuentas con registro (14-cuentas.md): correo y contraseña en la misma fila del jugador (la invitada
-- se convierte sin perder nada), sesiones que se pueden anular, enlaces de correo y entradas fallidas.
ALTER TABLE jugadores ALTER COLUMN token_dispositivo DROP NOT NULL;
ALTER TABLE jugadores ADD COLUMN correo TEXT;
ALTER TABLE jugadores ADD COLUMN hash TEXT;
ALTER TABLE jugadores ADD COLUMN verificado TIMESTAMPTZ;
ALTER TABLE jugadores ADD COLUMN google_id TEXT;
CREATE UNIQUE INDEX jugadores_correo ON jugadores (correo) WHERE correo IS NOT NULL;

CREATE TABLE sesiones (
  id BIGSERIAL PRIMARY KEY,
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  caduca TIMESTAMPTZ NOT NULL,
  anulada TIMESTAMPTZ
);
CREATE INDEX sesiones_jugador ON sesiones (jugador_id);

-- Enlaces de verificar el correo y de poner contraseña nueva. Se guarda el hash del token, no el token.
CREATE TABLE enlaces_cuenta (
  token_hash TEXT PRIMARY KEY,
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  tipo TEXT NOT NULL CHECK (tipo IN ('verificar', 'restablecer')),
  caduca TIMESTAMPTZ NOT NULL,
  usado TIMESTAMPTZ
);

CREATE TABLE intentos_entrada (
  correo TEXT PRIMARY KEY,
  fallos INT NOT NULL DEFAULT 0,
  bloqueado_hasta TIMESTAMPTZ
);
