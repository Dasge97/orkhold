-- Balance del juego editable desde el panel (10-panel-admin.md §1).
CREATE TABLE balance (
  clave TEXT PRIMARY KEY,
  valor DOUBLE PRECISION NOT NULL,
  por_defecto DOUBLE PRECISION NOT NULL,
  grupo TEXT NOT NULL,
  descripcion TEXT NOT NULL,
  minimo DOUBLE PRECISION NOT NULL,
  maximo DOUBLE PRECISION NOT NULL,
  actualizado TIMESTAMPTZ NOT NULL DEFAULT now(),
  actualizado_por TEXT
);

CREATE TABLE balance_historial (
  id BIGSERIAL PRIMARY KEY,
  clave TEXT NOT NULL,
  anterior DOUBLE PRECISION NOT NULL,
  nuevo DOUBLE PRECISION NOT NULL,
  quien TEXT NOT NULL,
  creado TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX balance_historial_creado ON balance_historial (creado DESC);
