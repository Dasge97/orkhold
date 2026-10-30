-- Panel de administración (10-panel-admin.md §2): administradores, bloqueo de jugadores, eventos,
-- avisos y ajustes del servidor (versión mínima del cliente, mantenimiento, jugadores en el pueblo).
CREATE TABLE administradores (
  id BIGSERIAL PRIMARY KEY,
  correo TEXT NOT NULL UNIQUE,
  hash TEXT NOT NULL,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  ultimo_acceso TIMESTAMPTZ
);

ALTER TABLE jugadores ADD COLUMN bloqueado TIMESTAMPTZ;
ALTER TABLE jugadores ADD COLUMN bloqueado_motivo TEXT;

CREATE TABLE eventos (
  id BIGSERIAL PRIMARY KEY,
  tipo TEXT NOT NULL,
  inicio TIMESTAMPTZ NOT NULL,
  fin TIMESTAMPTZ NOT NULL,
  parametros JSONB NOT NULL DEFAULT '{}',
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  creado_por TEXT NOT NULL
);

CREATE TABLE avisos (
  id BIGSERIAL PRIMARY KEY,
  titulo TEXT NOT NULL,
  texto TEXT NOT NULL,
  activo BOOLEAN NOT NULL DEFAULT true,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  creado_por TEXT NOT NULL
);

CREATE TABLE ajustes_servidor (
  clave TEXT PRIMARY KEY,
  valor TEXT NOT NULL,
  actualizado TIMESTAMPTZ NOT NULL DEFAULT now()
);
INSERT INTO ajustes_servidor (clave, valor) VALUES ('version_minima', '0'), ('mantenimiento', ''), ('jugadores_pueblo', '0');
