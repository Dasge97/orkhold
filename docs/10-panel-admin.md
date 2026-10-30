# 10 · Balance y panel de administración

Fecha: 2026-10-30. Bloque 2 de `09-plan-version-2.md`. Lo escribe el modelo con las decisiones de
Daniel del 30-10-2026 ("los números del juego se ajustan sin tocar código, desde un panel web").

## 1. La tabla de balance

### 1.1 Qué es
- Una tabla `balance` en PostgreSQL: una fila por número del juego.
- Cada fila: `clave` (texto, p. ej. `saqueo.acciones`), `valor` (número), `por_defecto`, `grupo`,
  `descripcion`, `minimo`, `maximo`, `actualizado`, `actualizado_por`.
- Los valores por defecto viven en `api/src/datos/balance.json`. Al arrancar, la API crea las filas
  que falten con su valor por defecto; nunca pisa un valor cambiado desde el panel.
- Todo cambio deja una fila en `balance_historial` (clave, valor anterior, valor nuevo, quién, cuándo).
  Desde el panel se puede volver a cualquier valor anterior o al valor por defecto.

### 1.2 Quién la usa
| Quién | Cómo |
|---|---|
| API | Lee la tabla en memoria al arrancar y tras cada cambio desde el panel. Todas las reglas (costes, límites, saqueo) salen de ahí. |
| Cliente (móvil) | `GET /balance` al arrancar y al volver al pueblo; guarda la última copia en el móvil. Muestra costes y tiempos con esos números. |
| Servidor de mundo | `GET /balance` al arrancar y cada 5 minutos: tiempos de reaparición. |

Los números que solo afectan a cómo se ve algo (tamaños, colores, cámara) no van en la tabla.

### 1.3 Contenido inicial
Valores de `01-diseno.md` y `08-formato-y-pvp.md`. Entre paréntesis, el mínimo y el máximo que acepta el panel.

**Saqueo** (`saqueo.*`)
| Clave | Valor | Qué es |
|---|---|---|
| `saqueos_dia` | 10 (1-100) | Saqueos por día |
| `hora_recarga` | 6 (0-23) | Hora del servidor (UTC) a la que se recargan |
| `escudo_horas` | 2 (0-48) | Horas sin poder ser elegido tras un saqueo con botín |
| `tasa` | 5 (0-100) | Monedas que paga el atacante al entrar |
| `acciones` | 3 (1-10) | Marcas en la fase 2 |
| `exploracion_segundos` | 120 (30-600) | Duración de la fase 1 |
| `mirar_segundos` | 1 (0-5) | Tiempo parado para mirar |
| `max_herramientas` | 2 (0-5) | Herramientas por saqueo |
| `margen_rival` | 100 (0-1000) | Diferencia máxima de puntuación con el rival |
| `horas_mismo_rival` | 24 (0-168) | Horas antes de poder saquear otra vez al mismo |
| `elo_nada_atacante` / `elo_nada_defensor` | -10 / 10 | Elo si no encuentra nada |
| `elo_una_atacante` / `elo_una_defensor` | 5 / -5 | Elo si acierta 1 marca |
| `elo_varias_atacante` / `elo_varias_defensor` | 15 / -10 | Elo si acierta 2 o más |
| `central_umbral` | 20 (0-1000) | Unidades en el cofre central que dan el castigo extra |
| `central_extra` | 5 (0-100) | Elo extra (+ atacante, - defensor) por ese castigo |
| `objetivo_exito_min` / `objetivo_exito_max` | 60 / 85 | % de saqueos con botín entre los que el panel no avisa (§2.3) |

**Escondites** (`escondite.<tipo>.*`, tipos `central`, `cofre`, `barril`, `enterrado`, `doble_fondo`)
| Tipo | `capacidad` | `porcentaje` | `limite` | Coste |
|---|---|---|---|---|
| central | 50 | 75 | 1 (fijo) | — |
| cofre | 30 | 50 | 4 | `coste.madera` 4, `coste.hierro` 1 |
| barril | 15 | 50 | 6 | `coste.madera` 2 |
| enterrado | 10 | 50 | 8 | `coste.piedra` 1 |
| doble_fondo | 10 | 50 | 0 (bloqueado hasta la mazmorra 2) | `coste.piedra` 3, `coste.hierro` 1 |

**Herramientas** (`herramienta.<id>.*`)
| Id | Coste | Otros | Disponible |
|---|---|---|---|
| farol | hierro 2, hueso 1 | `radio` 1 (casillas alrededor) | sí |
| cuarta_accion | moneda 10 | — | sí |
| pico | hierro 3, madera 2 | `segundos` 15, `dano` 3 | no (mazmorra 2) |
| bomba | hierro 4, piedra 2 | `retardo` 2 | no (mazmorra 2) |
| sabueso | hueso 3, moneda 2 | `casillas` 3 | no (mazmorra 3) |
| senuelo | hueso 2, madera 1 | — | no (mazmorra 3) |
Cada una tiene `herramienta.<id>.disponible` (0/1).

**Piezas del fuerte** (`pieza.<id>.*`): `vida` y `coste.<recurso>` de muro_madera (5; madera 2),
muro_piedra (10; piedra 3), puerta (6; madera 2, hierro 1), trampa (0; hierro 2), orco_defensor
(3; moneda 10), esqueleto_defensor (4; moneda 15), orco_jefe (15; unidad de botín).
Límites `fuerte.limite_muro` 40, `fuerte.limite_trampa` 6, `fuerte.limite_defensor` 4;
mejora `fuerte.mejora_huesos` 2, `fuerte.mejora_maxima` 3.

**Mundo** (`mundo.*`): `enemigos_reaparecer` 60 s, `recursos_reaparecer` 300 s.

**Mazmorra** (`mazmorra.*`): máximos por entrada `max_moneda` 60, `max_hierro` 10, `max_hueso` 12;
`segundos_entre` 20; jefe: `jefe_hueso` 3 y `jefe_hierro` 2 la primera vez del día, después
`jefe_monedas` 10.

**Pueblo** (`pueblo.*`): máximos por minuto `max_moneda` 30, `max_madera` 20, `max_piedra` 20,
`max_hierro` 10, `max_hueso` 10.

**Temporada** (`temporada.*`): `semanas` 4; tramos `plata` 1050, `oro` 1150, `hierro_orco` 1250
(por debajo de plata, bronce); `compresion` 50 (% de la distancia a 1000 que se conserva).

## 2. Panel de administración

### 2.1 Dónde vive
- Rutas `/admin/...` en la misma API, protegidas con sesión de administrador.
- La web del panel la sirve la propia API en `https://app.orkhold.com/admin/` (HTML, CSS y JS sin
  compilar, Vue 3 desde CDN). Así no hace falta otro dominio ni otro contenedor.
- Administradores en la tabla `administradores` (correo, contraseña con scrypt). El primero se crea
  con `orkhold-admin crear <correo>` en codehive, que genera la contraseña y la guarda en la bóveda
  como `orkhold - admin <correo>`. Sesión de 12 horas.
- Sigue la skill `diseno-web`: migas de pan arriba, subpestañas, modo claro y oscuro.

### 2.2 Pestañas
| Pestaña | Qué hace |
|---|---|
| Resumen | Jugadores totales, activos hoy y en el pueblo ahora; saqueos de hoy y % con botín; aviso si ese % sale del objetivo (§2.3). |
| Balance | Tabla por grupos, editable. Cada número con valor, por defecto, mínimo-máximo y fecha del último cambio. Botón "Volver al defecto". |
| Historial | Cambios de balance: quién, cuándo, de qué a qué, botón "Deshacer". |
| Jugadores | Buscar por nombre o correo. Ficha: inventario, fuerte (rejilla con escondites y contenido), saqueos hechos y recibidos, puntuación. Acciones: bloquear o desbloquear (con motivo), cambiar nombre. |
| Saqueos | Últimos 100. Filtro "sospechosos": aciertos en todas las marcas tres veces seguidas, entradas descartadas por el servidor o exploración más corta de 10 s con botín. |
| Eventos | Programar un evento: tipo, fecha de inicio y fin, parámetros (texto JSON). El juego los leerá cuando exista `12-eventos.md`. |
| Servidor | Versión mínima del cliente, mensaje de mantenimiento (si hay, el juego no deja entrar), jugadores en el pueblo. |
| Avisos | Mandar un aviso a todos: título y texto. El móvil lo enseña una vez al arrancar. |

### 2.3 Regla de las acciones (08 §4.6)
El resumen calcula el % de saqueos de los últimos 7 días que encontraron algo. Si es menor que
`saqueo.objetivo_exito_min`, sale "Sube saqueo.acciones"; si es mayor que `objetivo_exito_max`,
"Baja saqueo.acciones". El cambio lo hace una persona; el panel no lo cambia solo.

## 3. API nueva
| Ruta | Quién | Qué |
|---|---|---|
| `GET /balance` | todos | `{ version, valores: { clave: valor } }`. `version` cambia con cada cambio. |
| `GET /estado` | todos | Versión mínima del cliente, mantenimiento y avisos activos. |
| `POST /admin/sesion` | admin | Correo y contraseña → token de admin. |
| `GET/PUT /admin/balance`, `GET /admin/balance/historial`, `POST /admin/balance/deshacer` | admin | Balance. |
| `GET /admin/resumen`, `GET /admin/jugadores`, `GET /admin/jugadores/:id`, `POST /admin/jugadores/:id/bloquear`, `PUT /admin/jugadores/:id/nombre` | admin | Jugadores. |
| `GET /admin/saqueos` | admin | Saqueos con filtro. |
| `GET/POST/DELETE /admin/eventos`, `GET/PUT /admin/servidor`, `GET/POST /admin/avisos` | admin | Eventos, servidor y avisos. |
