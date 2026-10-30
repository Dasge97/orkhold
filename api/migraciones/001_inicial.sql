-- Tablas de 02-arquitectura.md §4.1 y las de control (mazmorras, botín del pueblo).
CREATE TABLE jugadores (
  id BIGSERIAL PRIMARY KEY,
  token_dispositivo TEXT NOT NULL UNIQUE,
  nombre TEXT,
  modelo TEXT,
  puntuacion INT NOT NULL DEFAULT 1000,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  ultimo_acceso TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX jugadores_nombre_unico ON jugadores (lower(nombre)) WHERE nombre IS NOT NULL;
CREATE INDEX jugadores_puntuacion ON jugadores (puntuacion DESC);

-- recurso: moneda | madera | piedra | hierro | hueso | orco_jefe (unidad de botín)
CREATE TABLE inventario (
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  recurso TEXT NOT NULL,
  cantidad INT NOT NULL DEFAULT 0 CHECK (cantidad >= 0),
  PRIMARY KEY (jugador_id, recurso)
);

CREATE TABLE fuertes (
  jugador_id BIGINT PRIMARY KEY REFERENCES jugadores(id) ON DELETE CASCADE,
  diseno_json JSONB NOT NULL,
  actualizado TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE asaltos (
  id BIGSERIAL PRIMARY KEY,
  atacante_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  defensor_id BIGINT REFERENCES jugadores(id) ON DELETE SET NULL,
  defensor_sistema TEXT,               -- fuerte del sistema (facil, medio, dificil) si no hay rival
  diseno_defensor_json JSONB NOT NULL,
  ganado BOOLEAN,
  segundos INT,
  muros_rotos INT,
  puntos_atacante_antes INT NOT NULL,
  puntos_atacante_despues INT,
  recompensa_json JSONB,
  creado TIMESTAMPTZ NOT NULL DEFAULT now(),
  terminado TIMESTAMPTZ
);
CREATE INDEX asaltos_atacante ON asaltos (atacante_id, creado DESC);
CREATE INDEX asaltos_defensor ON asaltos (defensor_id, creado DESC);

CREATE TABLE progreso (
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  clave TEXT NOT NULL,
  valor TEXT NOT NULL,
  PRIMARY KEY (jugador_id, clave)
);

-- Una fila por entrada a la mazmorra terminada (límites por entrada y frecuencia).
CREATE TABLE mazmorras (
  id BIGSERIAL PRIMARY KEY,
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  jefe_muerto BOOLEAN NOT NULL,
  recursos_json JSONB NOT NULL,
  creado TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Botín del pueblo enviado por el cliente (provisional hasta el servidor de mundo, fase 7).
CREATE TABLE botin_pueblo (
  id BIGSERIAL PRIMARY KEY,
  jugador_id BIGINT NOT NULL REFERENCES jugadores(id) ON DELETE CASCADE,
  recursos_json JSONB NOT NULL,
  creado TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX botin_pueblo_jugador ON botin_pueblo (jugador_id, creado DESC);
