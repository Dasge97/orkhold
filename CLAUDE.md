# Orkhold — instrucciones para el modelo

Juego de móvil en Unity 6 (6000.5.9f1): pueblo online low-poly compartido, mazmorras como mapas
aparte (los niveles) y un fuerte propio que se construye con lo recolectado y que otros jugadores
asaltan (el PvP). Daniel decide el diseño; el modelo implementa.

## Documentos que mandan
1. `docs/01-diseno.md` — qué es el juego y sus números. Si algo no cuadra, se cambia aquí primero.
2. `docs/02-arquitectura.md` — piezas, carpetas, formato de mapas, API, tablas.
3. `docs/03-tareas.md` — lista ordenada. Se trabaja **en orden**, una tarea cada vez, y se marca
   `[x]` con la fecha al cerrarla. No se empieza una fase sin cerrar la anterior.

## Cómo trabajar
- Al empezar una sesión: leer `docs/03-tareas.md`, coger la primera tarea sin marcar, decir cuál es,
  y leer la sección de `docs/01-diseno.md` y de `docs/02-arquitectura.md` que esa tarea cita.
- Una tarea = un ciclo: implementar → comprobar como dice la tarea → marcar `[x]` con fecha →
  anotar en `docs/05-diario.md` qué se hizo y qué se aprendió (3 líneas). Luego la siguiente.
- **Cuando dudes, no inventes.** Si la tarea no dice un número, una regla o un nombre, buscarlo
  primero en `01-diseno.md`. Si no está, parar y preguntar a Daniel en una línea con una propuesta.
  Nunca cambiar el diseño por cuenta propia.
- **Cuando algo falle dos veces** de la misma forma, parar, escribir en el diario qué se probó y
  preguntar. No seguir probando variantes a ciegas.
- Versiones fijas: Unity 6000.5.9f1; Netcode for GameObjects 2.13.1; Unity Transport 2.x (el que trae Netcode);
  Node 22; Fastify 5; PostgreSQL 16. No subir de versión sin que Daniel lo pida.
- Cada tarea debe dejar el proyecto compilando y la escena montándose. Si una tarea se queda a
  medias al acabar la sesión, se anota en el diario en qué punto está.
- Cada tarea termina con su comprobación hecha de verdad (no "debería funcionar"). Si la comprobación
  es visual, usar `Prototipo > Revisar` y mirar las fotos de `Revision/`.
- Código, comentarios, nombres de clases y de variables en **castellano**. Nombres de clips, packs y
  APIs de Unity en su idioma original.
- Un script por clase, en la subcarpeta de `Assets/Scripts/` que le toca.
- No tocar `Assets/Kenney/` (son packs CC0 tal cual).
- Commit y push al cerrar cada fase (pedido por Daniel el 29-10-2026).
- Para comprobar algo visual: `Prototipo > Revisar` hace fotos en `Revision/`; **mirarlas** con la
  herramienta de leer imágenes y describir lo que se ve antes de dar la tarea por buena.
- Para probar en el móvil: compilar APK, instalar en el emulador y hacer capturas con `adb`
  (comandos en la tabla de abajo). Los toques se simulan con `adb shell input tap X Y` y
  `adb shell input swipe X1 Y1 X2 Y2 MILISEGUNDOS`. Joystick en (256, 820), atacar en (2178, 866),
  interactuar en (1990, 900), correr en (2190, 665) a 2400x1080.

## Herramientas que ya existen
| Qué | Cómo |
|---|---|
| Montar las escenas desde cero | Menú `Prototipo > Montar escenas` (o borrar `Assets/Scenes/Pueblo.unity`; se monta sola en 2 s). Las escenas y `Assets/Mapas/Mallas/` se generan desde `Assets/Mapas/*.txt` y no van en git |
| Unity en batch (sin ventana) | `Unity.exe -batchmode -projectPath <proyecto> -logFile <log> -executeMethod <Clase.Metodo>`: `MontarEscenas.MontarTodas` (con `-quit`), `RevisionPrototipo.EnBatch`, `PruebaCargadorMapa.EnBatch`, `FotosModelos.EnBatch`, `CompilarAndroid.EnBatch` (con `-buildTarget Android`). No puede haber otro Unity con el proyecto abierto |
| API local | `cd api; docker compose up -d --build` (API en http://127.0.0.1:3100, PostgreSQL en 5442; secretos en `api/.env`, fuera de git). Tests: `docker compose run --rm api npm test` |
| APK contra la API local | `CompilarAndroid.EnBatch -urlApi http://10.0.2.2:3100` (10.0.2.2 = el PC visto desde el emulador) |
| Tests | `Unity.exe -batchmode -projectPath <proyecto> -runTests -testPlatform PlayMode -testResults <xml>`. Están en `Assets/Tests/PlayMode` |
| Ver forma de modelos | Rutas en `Revision/modelos.txt` y `FotosModelos.EnBatch`: hoja cenital (cubo rojo = norte), en perspectiva, y materiales |
| Revisión automática | Menú `Prototipo > Revisar`, o crear `Revision/pedir.txt` (se atiende sola en 2 s). Deja fotos de las cuatro escenas e `informe.txt` en `Revision/` |
| Compilar APK | Menú `Prototipo > Compilar APK`, o crear `Revision/compilar.txt`. Sale en `Builds/orkhold.apk` (IL2CPP, ARM64, OpenGL ES 3) |
| Probar en emulador | `emulator -avd Medium_Phone_API_36.0 -gpu host`, luego `adb install -r Builds/orkhold.apk`, `adb shell monkey -p com.orkhold.game 1`, `adb exec-out screencap -p > foto.png` |
| Registro de Unity | Unity escribe en `%LOCALAPPDATA%\Unity\Editor\Editor.log`; buscar `error CS`, `Exception`, `Escenas montadas`, `APK: resultado` |
| FPS en el móvil | `adb logcat -s Unity` muestra `FPS media=... escena=...` cada 5 s (`MedidorFps`) |
| Ejecutable de Windows | `CompilarEscritorio.WindowsEnBatch` (con `-buildTarget Win64`): `Builds/Windows/Orkhold.exe`. Argumentos: `-servidor -puerto N` (servidor de mundo), `-mundo host:puerto` (cliente), `-api URL`, `-nombre X`, `-bot` y `-bot-golpear` (jugador automático) |
| Prueba de red local | `powershell -File pruebas/red-local.ps1`: servidor y dos bots sin ventana; dice `RED: OK` si se ven moverse. Registros en `Revision/red/` |
| Servidor de mundo Linux | `CompilarEscritorio.ServidorLinuxEnBatch` (con `-buildTarget Linux64 -standaloneBuildSubtarget Server`): `Builds/Servidor/`. Necesita el módulo de Unity "Linux Dedicated Server Build Support". Despliegue en `deployment/` |
| Desplegar en codehive | `deployment/desplegar.sh` (Git Bash): sube API, compose y servidor de mundo, crea secretos, Traefik, cron y arranca. Scripts del servidor en `deployment/codehive/` |
| Icono y logo | `CrearIcono.EnBatch`: `Assets/Iconos/icono.png` y `logo.png`, y los pone en el proyecto |
| APK firmado | `pruebas/crear-keystore.sh` (una vez, con la bóveda desbloqueada) y `pruebas/compilar-apk-firmado.sh [url] [host:puerto]` |
| MCP de Unity | Paquete `com.coplaydev.unity-mcp` instalado. Arrancar desde `Window > MCP For Unity`; Claude Code lo tiene registrado como `UnityMCP` (http://127.0.0.1:8080/mcp) |

## Cosas aprendidas (no volver a tropezar)
- Unity no abre sin licencia activa; si falla en batchmode, abrir Unity Hub primero.
- Unity Hub por línea de comandos: quitar la variable `ELECTRON_RUN_AS_NODE` antes de llamarlo.
- Unity detecta cambios en los scripts solo cuando su ventana recibe el foco. Por eso existe
  `Assets/Editor/AutoRefrescar.cs`: cada 2 s mira si cambió algo y recarga solo, y atiende las
  marcas `Revision/pedir.txt` y `Revision/compilar.txt` y la escena borrada. Nadie tiene que tocar la ventana.
- Los FBX de Kenney traen los clips con nombre; el importador `ImportadorKenney` pone en bucle idle, walk y sprint.
- Unity 6 no compila x86_64 para Android. El emulador ejecuta ARM64 por traducción. Usar OpenGL ES 3: con Vulkan el emulador se cae.
- `adb` en Git Bash convierte `/sdcard/...` en una ruta de Windows; usar `exec-out` o `MSYS_NO_PATHCONV=1`.
- Las monedas y pociones de Mini Dungeon son enormes a escala 1; se ponen a 0,35 y 0,6.
- Medir fps en el emulador tras arrancarlo en frío (`-no-snapshot-load`): tras un rato encendido se llena de servicios de Android y baja a 15-25 fps.
- El emulador necesita 4 GB de RAM (`hw.ramSize` en `%USERPROFILE%\.android\avd\Medium_Phone.avd\config.ini`); con 2 GB va a 5-15 fps.
- Cada compilación de Unity reinicia `adb`; esperar a que `adb get-state` diga `device` antes de instalar.
- Las vallas del Nature Kit van en el borde de la casilla, no en el centro; el río y el puente, sin girar, van de norte a sur.
- Netcode for GameObjects va en la 2.13.1: la 2.4.x no compila con Unity 6000.5.
- En red, el `CharacterController` guarda su posición interna: al aparecer, apagarlo y encenderlo.
- Windows reserva puertos sueltos; el anfitrión local usa 21000-29000.
- Con `adb shell input swipe` largo, el dedo sigue pulsado tras cambiar de escena: usar gestos cortos cerca de puertas.
- Decisiones tomadas sin Daniel: `docs/06-decisiones.md`.
- En codehive Docker no tiene subredes libres: cada compose necesita una subred fija (Orkhold usa 10.96.0.0/24).
- El servidor de mundo bajo Wine: Xvfb lanzado a mano (no `xvfb-run`) y `-logFile` a un fichero, nunca `-`.
- El puerto UDP del servidor de mundo hay que abrirlo en el cortafuegos del panel de IONOS.
