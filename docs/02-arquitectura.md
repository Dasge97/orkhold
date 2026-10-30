# 02 · Arquitectura técnica

## 1. Piezas

```
┌──────────────────────┐    UDP (Netcode for GameObjects)    ┌──────────────────────────┐
│  Cliente Unity       │◄───────────────────────────────────►│  Servidor de mundo       │
│  (Android)           │                                      │  Unity headless, Linux   │
│                      │    HTTPS (JSON)                      │  contenedor en codehive  │
│                      │◄─────────────┐                       └───────────┬──────────────┘
└──────────────────────┘              │                                   │ HTTPS interno
                                      ▼                                   ▼
                          ┌──────────────────────────┐        ┌──────────────────────────┐
                          │  API (Node + Fastify)     │◄──────►│  PostgreSQL              │
                          │  cuentas, inventario,     │        │  contenedor en codehive  │
                          │  fuertes, asaltos, ranking│        └──────────────────────────┘
                          └──────────────────────────┘
```

| Pieza | Tecnología | Dónde corre | Para qué |
|---|---|---|---|
| Cliente | Unity 6, C#, render built-in, uGUI, entrada clásica | Móvil | Todo lo que se ve y se toca |
| Servidor de mundo | Unity 6 build "Dedicated Server" Linux + Netcode for GameObjects 2.x + Unity Transport | Docker en codehive | Solo el **pueblo**: posición de jugadores y enemigos, combate, quién recoge qué |
| API | Node 22 + Fastify + Zod | Docker en codehive | Cuentas, guardado, fuertes, elegir rival, resultado de asaltos, ranking |
| Base de datos | PostgreSQL 16 | Docker en codehive | Persistencia |
| Avisos | ntfy vía `kit-avisar` | codehive | Avisar al móvil de Daniel si algo cae |

**Qué corre dónde.** Solo el pueblo es compartido y pasa por el servidor de mundo. La mazmorra, el
fuerte propio y el asalto son **escenas locales** en el móvil (nadie más está dentro): se cargan con
transición y hablan solo con la API (cargar el diseño del fuerte, guardar botín y resultado).
Así el servidor de mundo es pequeño y la mayor parte del juego funciona aunque esté caído.

Por qué Netcode for GameObjects y no Mirror: es el oficial de Unity 6, va con Unity Transport y tiene
modo servidor dedicado. Esta primera versión tendrá decenas de jugadores, no miles.

Por qué una API aparte: acceder a PostgreSQL desde Unity es incómodo, y cuentas, fuertes y ranking
se prueban mejor en Node con tests normales. El servidor de mundo no guarda nada: avisa a la API.

## 2. Cliente

### 2.1 Carpetas
```
Assets/
  Kenney/            modelos y sprites (no tocar, CC0)
  Scripts/
    Entrada/         EntradaJuego, JoystickVirtual, BotonAccion
    Jugador/         ControlJugador, Vida, Inventario
    Enemigos/        Enemigo, EnemigoArquero, Jefe
    Mundo/           CargadorMapa, PaletaMapa, Recogible, Puerta, Fogata, Transicion
    Fuerte/          EditorFuerte, PiezaFuerte, DisenoFuerte (datos), Asalto
    Red/             ClienteRed (NGO), ApiCliente (HTTPS), Sesion
    UI/              HUD, PantallaConstruir, PantallaAsalto, PantallaRanking, Menu
  Editor/            MontarEscenas, CrearPrefabs, CrearPaletaMapa, CombinarMallas, ImportadorKenney,
                     RevisionPrototipo, CompilarAndroid, PruebaCargadorMapa, FotosModelos, AutoRefrescar
  Prefabs/           Jugador, Orco, Moneda, Pocion, Puerta, Casa, Interfaz (los genera CrearPrefabs)
  Mapas/             pueblo.txt, catacumbas.txt, fuerte-base.txt, PaletaMapa.asset
  Mapas/Mallas/      mallas fundidas de cada escena (generadas, fuera de git)
  Scenes/            Arranque, Pueblo, Mazmorra1, Fuerte, Asalto (generadas desde los mapas, fuera de git)
  Tests/PlayMode/    tests en Play Mode (Unity Test Framework)
  Animacion/         controladores generados
docs/                estos documentos
Builds/              APK (fuera de git)
```

### 2.2 Mapas en texto
Un fichero por mapa en `Assets/Mapas/`. Cada línea es una fila, cada letra una casilla de 1 m.
Cabecera `clave: valor`, luego `---`, luego la rejilla. Ejemplo:

```
nombre: pueblo
tipo: exterior
aparicion: F
---
TTTTTTTTTTTTTTTTTTTT
T........=.........T
T..r.....=....o....T
T........=.........T
T...F....+----B....T
T........=....C....T
T........=.........T
T........D....[]...T
TTTTTTTTTTTTTTTTTTTT
```

| Letra | Casilla |
|---|---|
| `.` | hierba (exterior) / suelo (mazmorra y fuerte) |
| `=` camino vertical, `-` horizontal, `+` cruce, `~` río, `_` puente |
| `#` muro, `T` árbol, `r` roca, `f` flor o hierba decorativa, `h` casa (bloque 2x2), `v` valla |
| `F` fogata (aparición), `C` cofre PvP, `B` tablón, `D` puerta de mazmorra, `E` salida o entrada (sin modelo) |
| `[` `]` esquinas noroeste y sureste del hueco del fuerte; `,` casilla reservada del hueco (sin suelo) |
| `o` orco, `a` arquero, `e` esqueleto, `J` jefe, `$` moneda, `p` poción, `X` trampa |
| `K` cofre del tesoro (solo en el fuerte) |

`CargadorMapa` lee el fichero y coloca las piezas según `PaletaMapa` (un `ScriptableObject` letra →
modelos + prefab con lógica). En el cliente coloca lo visual; en el servidor de mundo coloca colliders y enemigos. Los
dos leen el mismo fichero. Las piezas del **fuerte** no vienen del texto sino de `DisenoFuerte`
(§2.5), que se pinta encima de `fuerte-base.txt`.

Cabecera: `nombre`, `tipo` (`exterior` pone hierba; el resto, suelo de mazmorra), `aparicion` (letra
donde aparece el jugador) y `relleno: flores` (flores al azar en las casillas vacías).

Lo que sabe hacer la paleta, por letra:
- **Modelos** con variantes al azar (azar fijo por mapa: cliente y servidor eligen lo mismo).
- **Unión con vecinos**: el río elige recto, curva, te, cruce o final según las casillas de al lado;
  la valla pone un tramo del centro a cada vecino. El puente pone el río debajo y se gira para cruzarlo.
- **Entidad**: prefab con lógica (puerta, orco, moneda...) además del modelo.
- **Colisión**: caja o cápsula; muros, río y valla comparten una caja por tramo de fila.
- **Bloque**: `h` ocupa 2x2 y se pone una vez.

`MontarEscenas` (editor) monta cada escena desde su mapa y funde en pocas mallas todo lo fijo
(`CombinarMallas`, trozos de 16x16 casillas); árboles y rocas del interior quedan sueltos porque
se golpean. Las puertas guardan su id, su escena de destino y su casilla de llegada.

### 2.3 Transiciones
`Transicion.Ir("Mazmorra1")`: fundido a negro 0,5 s, `SceneManager.LoadSceneAsync`, fundido de
vuelta. Cada `Puerta` tiene `id`, `destino` y `llegarA` (id de la puerta de destino junto a la que
se aparece); `GestorEscena` coloca al jugador al cargar. Puertas: pueblo `D` → Mazmorra1, hueco del
fuerte → Fuerte, cofre `C` (botón) → Asalto; cada `E` vuelve al pueblo junto a su puerta. Pueblo, Mazmorra1, Fuerte y Asalto son escenas
separadas; solo Pueblo lleva `NetworkManager`.

### 2.4 Animación
Un AnimatorController por modelo con un estado por clip, sin transiciones. El código elige el
estado por nombre con `CrossFadeInFixedTime`. Clips en bucle: idle, walk, sprint.

### 2.5 Diseño del fuerte (datos)
```json
{ "version": 1, "piezas": [ { "x": 3, "z": 7, "tipo": "muro_madera", "vida_extra": 0 }, ... ] }
```
`DisenoFuerte` es una clase serializable. Se guarda en la API y se carga para el propio fuerte y
para el asalto. `EditorFuerte` lo edita (poner, quitar, validar límites y coste). `Asalto` lo
instancia con colliders y con `Enemigo` en los defensores.

### 2.6 Red en el cliente
- `ClienteRed`: conecta con NGO al servidor de mundo en la escena Pueblo. El jugador es un
  `NetworkObject` con autoridad del servidor: el cliente manda la entrada, el servidor mueve.
  Predicción local del propio movimiento y corrección suave.
- `ApiCliente`: `UnityWebRequest`, JSON con `JsonUtility`, token en cabecera. URL en
  `Resources/Configuracion.asset` (vacía = modo local, como en los tests); el APK se compila con
  `-urlApi <url>`.
- `Sincronizacion`: la API manda y la partida local es caché (pueblo en lotes de 5 s, mazmorra al
  salir, fuerte en "Listo", asalto al buscar y al terminar).
- `Sesion`: token de dispositivo en `PlayerPrefs`. Sin contraseña en la primera versión.
- Sin conexión: el pueblo arranca en modo host local (NGO) y se avisa en pantalla; mazmorra sí,
  fuerte y asalto no (necesitan la API).

## 3. Servidor de mundo
- Mismo proyecto Unity, build "Dedicated Server" Linux x86_64. Solo carga la escena Pueblo.
- Enemigos solo en el servidor; el cliente recibe posición, animación y vida.
- Cuando un jugador recoge o mata, el servidor llama a la API (`POST /inventario/sumar`) con una
  clave de servidor; el cliente nunca suma inventario por su cuenta en el pueblo.
- Cada 30 s manda `kit-estado` con jugadores conectados.
- Docker: puerto UDP 7777; reinicio automático.

## 4. API
- Node 22 + Fastify. Validación con Zod. Tests con `node:test`.

| Método y ruta | Qué hace |
|---|---|
| `POST /sesion` | Crea o recupera cuenta por token de dispositivo. Devuelve token de sesión. |
| `GET /jugador` | Personaje, inventario, puntuación, progreso. |
| `PUT /jugador/personaje` | Nombre y modelo elegido. |
| `POST /inventario/sumar` | Solo servidor de mundo (clave de servidor, cabecera `X-Clave-Servidor`): `{jugador, recurso, cantidad, origen}`. |
| `POST /pueblo/botin` | **Provisional hasta la fase 7**: el cliente manda lo recogido en el pueblo (`{recursos}`), con límite por minuto. Se apaga con `PUEBLO_CLIENTE=0`. |
| `POST /mazmorra/resultado` | Cliente: botín de una entrada a la mazmorra (`{jefe_muerto, recursos}`). Validado contra límites por entrada. |
| `GET /fuerte` · `PUT /fuerte` | Diseño del fuerte propio. `PUT` valida límites, coste y que las piezas sean suyas; descuenta recursos. |
| `GET /catalogo` | Piezas del fuerte con coste y vida. |
| `POST /asalto/buscar` | Elige rival ±100 puntos (o un fuerte del sistema si no hay), devuelve `{asalto_id, rival, diseño}`. Máximo 10 al día. |
| `POST /asalto/resultado` | `{asalto_id, ganado, segundos, muros_rotos}`. Comprueba 5-90 s, aplica Elo y recompensas, guarda. |
| `GET /asaltos/historial` | Últimos 10 hechos y recibidos. |
| `GET /ranking` | 50 mejores y tu puesto. |
| `GET /salud` | Para el Kit. |

Detalles (implementado en `api/`, tests en `api/test`):
- Sesión: token firmado HMAC-SHA256 de 30 días (`Authorization: Bearer`). Errores: `{error, mensaje}`.
- `PUT /fuerte` cobra la diferencia entre el diseño nuevo y el viejo: coste de piezas + 2 huesos por
  mejora + unidades `orco_jefe`; lo quitado se devuelve entero. Reglas iguales a `ReglasFuerte.cs`
  (copia de `fuerte-base.txt` y catálogo en `api/src/datos`; un test de Unity comprueba que coinciden).
- Mazmorra: máximo por entrada 60 monedas, 10 hierro, 12 huesos, nada de madera ni piedra; una
  entrada cada 20 s; si `jefe_muerto`, la API da la unidad (primera vez) o 3 huesos.
- Asalto: rival con fuerte de ±100 puntos, si no fuerte del sistema (fácil < 1000 ≤ medio < 1100 ≤
  difícil, con 900/1000/1100 puntos). Elo: ganar `40·(1−E)`, perder `−30·E` (+20/−15 entre iguales);
  el defensor no cambia. Victoria entre 5 y 90 s y no más segundos de los pasados desde la búsqueda.
- Inventario de la API: `moneda, madera, piedra, hierro, hueso, orco_jefe`. Las pociones solo existen en el móvil.

### 4.1 Tablas
```
jugadores(id, token_dispositivo, nombre, modelo, puntuacion, creado, ultimo_acceso)
inventario(jugador_id, recurso, cantidad)            -- moneda|madera|piedra|hierro|hueso
fuertes(jugador_id, diseno_json, actualizado)
asaltos(id, atacante_id, defensor_id, diseno_defensor_json, ganado, segundos, muros_rotos,
        puntos_atacante_antes, puntos_atacante_despues, creado)
progreso(jugador_id, clave, valor)                   -- ej. "jefe1_muerto" = "1"
asaltos: además defensor_sistema, recompensa_json y terminado (resultado pendiente si es NULL)
mazmorras(id, jugador_id, jefe_muerto, recursos_json, creado)      -- control de entradas
botin_pueblo(id, jugador_id, recursos_json, creado)                -- provisional (fase 7)
```

## 5. Despliegue en codehive
- Carpeta `deployment/` con `docker-compose.yml`: `postgres`, `api`, `mundo`. Red interna; la API
  sale por el proxy existente (443) y el mundo por UDP 7777.
- Credenciales en la bóveda Vaultwarden (`juego - postgres`, `juego - api secreto`, `juego - clave servidor mundo`).
- `kit-estado juego-api` y `kit-estado juego-mundo`; añadir los dos al paquete `kit-codehive`.
- Subdominio *propuesta*: `orkhold.code-hive.space` para la API (y el mismo host para UDP 7777). Dominio propio `orkhold.app` cuando se publique.

## 6. Pruebas
- Cliente: `RevisionPrototipo` (fotos + Play automático) se amplía con un caso por pantalla.
- API: tests de cada ruta con base de datos de prueba.
- Red: servidor local + 2 clientes en editor (ParrelSync) o 2 emuladores.
- APK: `CompilarAndroid` + emulador Medium Phone con `adb`.
