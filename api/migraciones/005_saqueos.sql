-- Saqueo (08-formato-y-pvp.md). Sustituye a los asaltos de 90 s (la tabla asaltos queda como historia).
-- Un saqueo pasa por: preparado (rival elegido) → explorando (tasa y herramientas cobradas) → terminado.
CREATE TABLE saqueos (
  id BIGSERIAL PRIMARY KEY,
  atacante_id BIGINT REFERENCES jugadores(id) ON DELETE SET NULL,
  defensor_id BIGINT REFERENCES jugadores(id) ON DELETE SET NULL,
  prueba BOOLEAN NOT NULL DEFAULT false,          -- fuerte de prueba: sin botín real, tasa ni Elo
  diseno_defensor_json JSONB NOT NULL,            -- el fuerte del defensor al preparar (con contenido)
  estado TEXT NOT NULL DEFAULT 'preparado' CHECK (estado IN ('preparado', 'explorando', 'terminado')),
  herramientas JSONB NOT NULL DEFAULT '[]',
  tasa INT NOT NULL DEFAULT 0,
  acciones INT NOT NULL DEFAULT 0,
  empezado TIMESTAMPTZ,
  miradas JSONB NOT NULL DEFAULT '[]',            -- [{ x, z, t, respuesta }]
  marcas_json JSONB,                              -- [{ x, z, resultado, cantidades }]
  marcas INT,
  encontrado INT,                                 -- marcas con botín
  descartadas INT,                                -- entradas de la exploración que el servidor no aceptó
  segundos_exploracion INT,
  rotos_json JSONB,                               -- casillas de muros rotos [[x, z], ...]
  entradas_json JSONB,                            -- lo que mandó el móvil (para el informe y revisiones)
  botin_json JSONB,
  puntos_atacante_antes INT,
  puntos_atacante INT,                            -- cambio de puntuación del atacante
  puntos_defensor_antes INT,
  puntos_defensor INT,                            -- cambio de puntuación del defensor
  visto_defensor BOOLEAN NOT NULL DEFAULT false,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  terminado TIMESTAMPTZ
);
CREATE INDEX saqueos_atacante ON saqueos (atacante_id, creado DESC);
CREATE INDEX saqueos_defensor ON saqueos (defensor_id, creado DESC);

ALTER TABLE jugadores ADD COLUMN escudo_hasta TIMESTAMPTZ;
ALTER TABLE jugadores ADD COLUMN estandarte TEXT;

CREATE TABLE temporadas (
  numero INT PRIMARY KEY,
  inicio TIMESTAMPTZ NOT NULL,
  fin TIMESTAMPTZ NOT NULL,
  cerrada TIMESTAMPTZ
);
