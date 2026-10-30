# 05 · Diario de implementación

Una entrada por tarea cerrada o por sesión. Tres líneas: qué se hizo, cómo se comprobó, qué se aprendió.
Lo más reciente arriba.

## 2026-10-30 — Fase 9 (9.1 a 9.4; 9.5 preparada)
- Hecho: icono y logo generados (`CrearIcono`), pantalla de carga con consejos, 5 carteles de tutorial
  (`Cartel`, letras 1-5 del mapa), sin sombras en suelo y flores, LOD en árboles y rocas, README nuevos
  con 7 capturas; firma del APK con almacén propio preparada.
- Comprobado: 44/44 tests (uno nuevo del tutorial); en el emulador, arranque, carga, cartel y mazmorra
  vistos en capturas; 59-60 fps de media en el pueblo (antes 56-59).
- Aprendido: un gesto largo con `adb` sigue pulsado tras cambiar de escena y mete al jugador por la
  puerta de vuelta; las fotos de los tests no muestran la interfaz superpuesta.

## 2026-10-30 — Fase 8 preparada, sin desplegar
- Hecho: `deployment/` completo (compose, Dockerfile Linux y con Wine, ruta de Traefik, `desplegar.sh`,
  `orkhold-secretos`, `orkhold-vigilar`, `orkhold-copia`); el servidor de mundo escribe su estado cada
  30 s; `-mundoApk` al compilar el APK; `juego-api` y `juego-mundo` en `kit-codehive/kit.json`.
- Comprobado: 43/43 tests; el fichero de estado se escribe con el ejecutable de Windows. El despliegue
  no se lanzó: el control de permisos lo paró por tocar codehive, y faltan el DNS y la bóveda.
- Aprendido: codehive publica cada proyecto con un fichero de Traefik en `infrastructure/traefik/dynamic`
  y un puerto 41xx en 127.0.0.1; el DNS de `code-hive.space` está en IONOS y no tiene comodín.

## 2026-10-30 — Fase 7 (7.1 a 7.6 y 7.8; 7.7 a medias)
- Hecho: pueblo online con Netcode 2.13.1: `RedPueblo` (servidor, cliente o anfitrión local),
  `JugadorRed` (entrada al servidor, predicción y corrección), `EnemigoRed`, `RecogibleRed`,
  `MundoRed`, `NombreEncima`, `ApiServidor`; ejecutable de Windows para pruebas y `BotRed`;
  `deployment/` con compose y Dockerfile del mundo. 7.7 queda a medias: el módulo de servidor Linux
  de Unity pide permisos de administrador y la ventana caducó dos veces.
- Comprobado: 43/43 tests de Unity; `pruebas/red-local.ps1` (servidor y dos bots: se ven moverse en
  13-14 de 14 informes); un bot con `-bot-golpear` tala, mata orcos y recoge, y la API guarda su
  madera y monedas (consulta a PostgreSQL).
- Aprendido: la 2.4.x de Netcode no compila con Unity 6000.5; el `CharacterController` guarda una
  posición interna que hay que reiniciar al aparecer en red; Windows reserva puertos sueltos (el
  anfitrión usa 21000-29000); un `MonoBehaviour` que simula toques tiene que ir antes del que los limpia.

## 2026-10-30 — Fase 6 cerrada (tareas 6.1 a 6.9)
- Hecho: API en `api/` (Node 22, Fastify 5, Zod, pg, PostgreSQL 16 en docker compose, migraciones
  numeradas, 31 tests con `node:test`); cliente con `Configuracion`, `ApiCliente`, `Sesion`,
  `Sincronizacion`, escena Arranque (crear personaje con 4 modelos de Mini Characters),
  `PersonajeJugador`, tablón con ranking e historial, asalto real contra la API.
- Comprobado: 31/31 tests de la API, 39/39 de Unity (uno juega entero contra la API en Docker); en el
  emulador: Ana crea personaje y construye 5 muros (la API descuenta la madera), Bruno busca rival
  (le toca Ana), gana en 69 s (+20, recompensas de la API), el ranking cambia y Ana tiene el asalto en
  "recibidos". Con un solo emulador y dos cuentas seguidas: no hay disco para un segundo emulador.
- Aprendido: un POST sin cuerpo desde Unity llega con otro Content-Type (415 en Fastify); en
  PostgreSQL un upsert con cantidad negativa falla la comprobación antes del conflicto; `extract()`
  llega como texto. `il2cpp.exe` se cerró una vez solo (AccessViolation); al repetir, compiló.

## 2026-10-30 — Fase 5 cerrada (tareas 5.1 a 5.5)
- Hecho: `Asalto` (90 s, ganar tocando el cofre, perder al morir, por tiempo o saliendo, muros rotos,
  recompensas y puntuación locales), `PantallaAsalto` (reloj, barra al cofre, resultado),
  `BuscarRival` en el cofre PvP con los tres fuertes de prueba en JSON, y `BotAsalto` para la 5.5.
- Comprobado: 6 tests nuevos (35 de 35 OK); el jugador automático gana el difícil en 67-70 s y lo
  pierde jugando mal (`docs/04-pruebas-asalto.md`); en el emulador, cofre → panel → difícil → asalto
  con reloj y barra a 59 fps.
- Aprendido: con las vidas del diseño (3 y 6 golpes) y un golpe que rompía varios muros, un asalto
  no pasaba de 20 s; un golpe = una pieza y vidas 5/10/6. Falta la prueba con Daniel.

## 2026-10-30 — Fase 4 cerrada (tareas 4.1 a 4.6)
- Hecho: unidades defensoras en la partida; `CatalogoPiezas` (7 piezas con coste, vida y límites);
  `DisenoFuerte`, `ReglasFuerte`, `ConstructorFuerte`, `Fuerte` (propio, asalto y vista en el hueco
  del pueblo); `EditorFuerte` + `PantallaConstruir` (barra de piezas, quitar al tocar, mejorar con
  hueso, Listo); `PiezaFuerte` (muros que se rompen, puerta que abre el dueño, trampa oculta,
  defensores); `Guardado` en `partida.json`; `Menu` con sonido, vibración y reiniciar.
- Comprobado: 10 tests nuevos (29 de 29 OK), entre ellos "5 muros, cerrar, abrir: siguen ahí y la
  madera descontada"; en el emulador se anda hasta el fuerte, se entra a construir con un toque y la
  partida se carga del disco al volver a abrir; 59 fps en el fuerte.
- Aprendido: el emulador se degrada tras un rato encendido (memoria llena de servicios de Android,
  14-24 fps en una escena sencilla); medir fps siempre tras un arranque en frío.

## 2026-10-30 — Fase 3 cerrada (tareas 3.1 a 3.6)
- Hecho: `Enemigo` con estados (quieto, patrulla, persigue, ataca, vuelve, muerto), botín y
  reaparición a los 60 s; `EnemigoArquero` + `Proyectil`; esqueleto (misma clase, otros números y
  material oscuro); `Jefe` con golpe en área avisado (`AvisoArea`) y `BarraVidaJefe`; `Trampa` con
  las animaciones show/hide del modelo; botín del jefe (unidad la primera vez, 3 huesos después).
  Los orcos del pueblo patrullan 3 casillas.
- Comprobado: 9 tests nuevos (19 de 19 OK), entre ellos "matar jefe" con vida infinita que acaba con
  la unidad en el inventario y la vuelta al pueblo; fotos 43-45.
- Aprendido: un arquero que huye en línea recta se queda pegado al muro en las esquinas; hay que
  buscar una dirección libre (esfera lanzada a 1 m) antes de moverse.

## 2026-10-29 — Fase 2 cerrada (tareas 2.1 a 2.6)
- Hecho: `Partida` (inventario que vive entre escenas, con moneda, madera, piedra, hierro, hueso y
  pociones), `Regeneracion`, `Fogata`, `FuenteRecurso` (árboles y rocas: 3 golpes, temblor, se agotan
  60 s), `IGolpeable`, `EfectoDano` + `NumeroFlotante`, muerte por escena en `GestorEscena`,
  `BotonContexto`, `Avisos` y `Sonidos` (Kenney RPG Audio e Impact Sounds, 48 clips).
- Comprobado: 7 tests nuevos en Play Mode (golpear árbol → 1 madera, regenerar, fogata, parpadeo,
  muerte en pueblo y en mazmorra, icono del botón, sonidos), 10 de 10 OK; fotos 40-42; APK en el
  emulador a 60 fps.
- Aprendido: tintar con `_Color` multiplica la textura y deja al personaje casi negro; para el
  parpadeo hay que cambiar el material entero. Los objetos marcados estáticos no se pueden mover ni
  escalar en juego: árboles y rocas sueltos no se marcan.

## 2026-10-29 — Fase 1 cerrada (tareas 1.2 a 1.7)
- Hecho: `MontarEscenas` monta Pueblo, Mazmorra1, Fuerte y Asalto desde `pueblo.txt` (48x48),
  `catacumbas.txt` (20x30) y `fuerte-base.txt` (16x16), con prefabs (`CrearPrefabs`), puertas con
  `Transicion` y `GestorEscena`, colisiones unidas por fila y mallas fundidas por trozos (`CombinarMallas`).
  Nature Kit completo y Mini Characters importados. Tests en Play Mode con Unity Test Framework 1.7.0.
- Comprobado: 3 tests de transición OK (mazmorra, fuerte, asalto desde el cofre); fotos de `Revisar`
  de las cuatro escenas; en el emulador se anda de la fogata a la mazmorra con el joystick y entra;
  60 fps estables en Pueblo y en Mazmorra1.
- Aprendido: el emulador tenía 2 GB de RAM e iba a 5-15 fps por memoria de intercambio, incluso con
  una escena vacía a 28 fps; con 4 GB (`hw.ramSize` en `Medium_Phone.avd/config.ini`) da 60 fps.
  Los pulsos de botón deben leerse y borrarse en el mismo `Update` del jugador o se pierden.

## 2026-10-29 — Tarea 1.1: PaletaMapa y CargadorMapa
- Hecho: `MapaTexto` (cabecera + rejilla), `PaletaMapa` (letra → modelos, colisión, bloque NxN, marca),
  `CargadorMapa` (lee y coloca; las letras con lógica propia vuelven como marcas). La paleta se crea
  con `Prototipo > Crear paleta de mapas` en `Assets/Mapas/PaletaMapa.asset`.
- Comprobado: `Prototipo > Probar cargador de mapas` (o batch `PruebaCargadorMapa.EnBatch`) con el
  mapa de ejemplo de la arquitectura y uno interior: 12 de 12 comprobaciones OK y fotos 20 y 21 bien.
  De paso, Unity abre y compila desde la ruta nueva (solo avisos CS0618 que ya existían).
- Añadido después, a petición de Daniel: 34 modelos de río, puente y cascada del Nature Kit 2.1
  (zip de la investigación) en `Assets/Kenney/NatureKit/`. El río elige recto, curva, te, cruce o
  final según sus vecinos; el puente pone el río debajo y se gira para cruzarlo. Prueba: 20 de 20 OK.
- Aprendido: sin `Library`, el primer batch tarda varios minutos. A giro 0, las piezas de río del
  Nature Kit unen: recto norte-sur, curva sur-este, final sur, split oeste-este-sur.

## 2026-10-29 — Fase 0 cerrada
- Hecho: proyecto movido a `C:\AreaDeTrabajo\JUEGOS\orkhold` (sin `Library`, se regenera); repo git;
  README en inglés y español con capturas reales en `docs/img/`; scripts en subcarpetas
  (Entrada, Jugador, Enemigos, Mundo, UI); nombre Orkhold y id `com.orkhold.game`.
- Comprobado: pendiente abrir Unity desde la ruta nueva y confirmar que compila y monta la escena
  (lo hace la primera sesión de la fase 1 antes de empezar).
- Aprendido: el Hub y el cliente de licencias bloquean la carpeta del proyecto aunque Unity esté
  cerrado; copiar con robocopy sin `Library` es lo más rápido.

## 2026-10-29 — Prototipo inicial (antes de la lista de tareas)
- Hecho: proyecto Unity montado a mano; escena PvE generada por `MontarEscenaPvE`; jugador, 6 orcos,
  monedas, pociones, joystick, 3 botones, HUD. APK compilado y probado en el emulador Medium Phone.
- Comprobado: `Prototipo > Revisar` (fotos + Play automático) y capturas del emulador con `adb`.
- Aprendido: ver "Cosas aprendidas" en `CLAUDE.md`.

## 30-10-2026 — tarea 9.5: APK firmado
- Almacén creado con `pruebas/crear-keystore.sh`; contraseña en la bóveda («orkhold - keystore»). APK firmado comprobado con apksigner: CN=Orkhold.
- `vault-usar` exige nombres de variable de dos letras o más (su patrón `[A-Za-z_][A-Za-z0-9_]*` en un `case`): «V» fallaba, ahora «CLAVE».
- El módulo Linux de Unity sigue sin instalar (la ventana de permisos caducó otra vez) y el despliegue lo bloquea el modo automático: los lanza Daniel.

## 30-10-2026 — fase 8: despliegue en codehive
- Desplegado en https://app.orkhold.com (cambio de Daniel). API en verde en el Kit; secretos en la bóveda («juego - postgres», «juego - api secreto», «juego - clave servidor mundo»).
- Tropiezos: Docker sin subredes libres (subred fija 10.96.0.0/24); Wine necesita Xvfb; `xvfb-run` se cuelga como PID 1; `-logFile -` impide arrancar; `orkhold-vigilar` tenía CRLF en la copia de trabajo.
- Los bots desde el PC no llegan al servidor de mundo: tcpdump en codehive no ve ningún paquete UDP 7777. Hay que abrir el puerto en el cortafuegos de IONOS.
- Puerto UDP 7777 abierto en IONOS: dos bots desde el PC contra app.orkhold.com se ven moverse (RED: OK).

## 30-10-2026 — fase 10: balance y panel (10.1-10.3)
- Tabla `balance` con 101 números, `GET /balance`; reglas de la API, catálogo y jefe leen de ahí. Rutas `/admin` con tokens propios y bloqueo de jugadores.
- Panel en `/admin/` (Vue desde CDN, sin compilar), capturas con Chrome sin ventana y puppeteer-core en la carpeta temporal: escritorio y móvil, claro y oscuro.
- Aprendido: la imagen de la API copia `src` al construir; antes de `npm test` hay que `docker compose build api`.
- 10.4: `Balance.cs` descarga `GET /balance` (en forma de lista: JsonUtility no lee diccionarios), lo guarda en el móvil y lo aplica a una copia en memoria del catálogo (cambiar el asset en Play Mode lo ensucia). Enemigos y árboles del pueblo toman su tiempo al arrancar, porque en el pueblo los crea la red después de cargar la escena.

## 30-10-2026 — fase 11: cuentas con registro
- API: `/cuenta/...` con scrypt, tabla `sesiones` (el token lleva su id y se puede anular), enlaces de un solo uso guardados con hash, 5 intentos y 15 min, filtro de nombres. Sin SMTP el enlace va al registro.
- Juego: `Sesion` guarda el token en el móvil; `PantallaCuenta` para entrar, guardar el progreso, cerrar sesión y borrar. El menú cambia "Reiniciar partida" por "Cuenta" cuando hay servidor.
- Aprendido: un 401 por contraseña mala no es una sesión caducada (`ApiCliente` solo cierra con `sin_sesion`). Las fotos de los tests ya sacan la interfaz: los lienzos se ponen un momento delante de la cámara.

## 30-10-2026 — fase 12: saqueo
- API: escondites dentro del diseño (`escondites.js`), `saqueo.js` rehace la exploración y resuelve marcas, rutas `/saqueo`, herramientas y temporadas. Se quitan las rutas del asalto.
- Juego: `Saqueo` (explorar, mirar, marcar, resultado), `PantallaPrepararSaqueo`, `PantallaInforme`, `PantallaReparto`, `PantallaFerra`; escondites en el modo construir.
- Aprendido: JsonUtility manda siempre los campos de clase (un `golpe` vacío llegaba como (0,0) y el servidor descartaba todo): hace falta un booleano que diga si va de verdad. Para los tests de Unity, la API local acepta un rival por nombre (`PRUEBAS=1`, solo en local).

## 30-10-2026 — fase 13: lore en el juego
- Los cinco carteles los dicen Maela, Rook, Ferra (con la forja), Ansel y Tobin, con su modelo de Mini Characters delante; Gruk con sus frases al este. Textos de carga del lore y "Vado Gris" en la carga del pueblo. Urzog habla al empezar el combate, al caer y en tu fuerte.
- 42 tests de Unity y 55 de la API en verde.

## 30-10-2026 — versión 2 en producción
- Copia de la base antes de subir; `desplegar.sh` aplicó las migraciones 002-005. API, panel y balance responden en app.orkhold.com; dos bots se ven en el servidor de mundo (RED: OK).
- Administrador `admin@orkhold.com` con su contraseña en la bóveda («orkhold - admin admin@orkhold.com»). APK firmado de la versión 2 en `Builds/orkhold.apk`.
- No se probó en el emulador: el disco C tiene menos de 2 GB libres y el emulador no arranca.
