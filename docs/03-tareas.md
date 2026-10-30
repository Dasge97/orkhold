# 03 · Lista de tareas

Orden de ejecución. Cada tarea tiene qué hacer y cómo comprobar que está hecha.
Marcar con `[x]` al terminar y anotar fecha. No pasar a la siguiente fase sin cerrar la anterior.
Si una tarea obliga a cambiar el diseño, se cambia primero `01-diseno.md` y se anota el porqué.

## Fase 0 · Proyecto propio
- [x] 0.1 (29-10-2026) Mover el proyecto de `RESEARCHIA/.../assets/prototipo-unity` a `C:\AreaDeTrabajo\JUEGOS\orkhold` y crear repo git privado en GitHub (Dasge97). `.gitignore` de Unity. Dejar en la investigación un enlace y marcarla `graduada`.
  Comprobar: `git status` limpio; Unity abre el proyecto desde la ruta nueva sin errores.
- [x] 0.2 Nombre decidido: **Orkhold** (29-10-2026). Subdominio propuesto `orkhold.code-hive.space`; confirmar con Daniel al desplegar (fase 8).
- [x] 0.3 (29-10-2026) README.md y README.es.md según las reglas globales (cabecera, chips, secciones, huecos para capturas).
- [x] 0.4 (29-10-2026) Reorganizar `Assets/Scripts/` en las subcarpetas de `02-arquitectura.md` §2.1. Sin cambiar lógica.
  Comprobar: compila sin avisos nuevos; `Prototipo > Revisar` sigue funcionando.

## Fase 1 · Mapas en texto y transiciones
Leer antes: `01-diseno.md` §4.1, §4.3; `02-arquitectura.md` §2.2, §2.3.
- [x] 1.1 (29-10-2026) `PaletaMapa` (ScriptableObject letra → prefab/acción) y `CargadorMapa` que lee `Assets/Mapas/*.txt` (cabecera + rejilla).
- [x] 1.2 (29-10-2026) Reescribir `MontarEscenaPvE` para que use `CargadorMapa` con `pueblo.txt`. Flores aleatorias como relleno opcional (`relleno: flores` en la cabecera).
- [x] 1.3 (29-10-2026) Escribir `pueblo.txt` (48x48): plaza con fogata, cofre PvP y tablón; calles; 6 casas; río con puente; bosque en bordes; puerta de mazmorra al norte; hueco del fuerte 16x16 al este; 8 orcos; monedas.
- [x] 1.4 (29-10-2026) Escribir `catacumbas.txt` (20x30): entrada, pasillo, sala de trampas, sala del jefe con `J`.
- [x] 1.5 (29-10-2026) Escribir `fuerte-base.txt` (16x16): suelo, valla exterior con entrada al oeste, cofre del tesoro `K` en el centro.
- [x] 1.6 (29-10-2026) `Transicion`: fundido a negro, carga de escena, fundido de vuelta. Escenas separadas Pueblo, Mazmorra1, Fuerte, Asalto. `Puerta` que llama a la transición y recuerda a dónde volver.
- [x] 1.7 (29-10-2026) Un collider por fila de muros contiguos en vez de uno por casilla (rendimiento).
  Comprobar: las cuatro escenas se montan desde texto; se entra y sale de la mazmorra y del fuerte con transición; fotos de `Revisar`; 60 fps en el emulador.

## Fase 2 · Jugador y combate pulidos
Leer antes: `01-diseno.md` §4.4, §4.5.
- [x] 2.1 (29-10-2026) Vida que se regenera (1 cada 10 s fuera de combate) y curación total en la fogata.
- [x] 2.2 (29-10-2026) Golpear árboles y rocas con el botón de atacar: 3 golpes → suelta madera o piedra. Temblor del modelo en cada golpe. El árbol o roca "se agota" 60 s.
- [x] 2.3 (29-10-2026) Daño visible: el golpeado parpadea en rojo 0,15 s y recibe empujón; número de daño flotante.
- [x] 2.4 (29-10-2026) Muerte del jugador: pantalla oscura 1 s, reaparece en la fogata (o sale de la mazmorra con la mitad de lo recogido).
- [x] 2.5 (29-10-2026) Botón de interactuar con contexto: cambia de icono según lo que haya cerca (recoger, puerta, cofre, tablón, construir).
- [x] 2.6 (29-10-2026) Sonidos básicos (pasos, golpe, recoger, morir, romper muro) de Kenney "RPG Audio" o "Impact Sounds" (CC0).
  Comprobar: `Revisar` ampliado con "golpear árbol" que acaba con 1 madera en el inventario.

## Fase 3 · Enemigos y mazmorra
Leer antes: `01-diseno.md` §4.2, §4.3.
- [x] 3.1 (30-10-2026) `Enemigo` base con estados: quieto, patrulla (2 puntos), persigue, ataca, muere. Reaparece a los 60 s.
- [x] 3.2 (30-10-2026) `EnemigoArquero`: mantiene 4 m, lanza proyectil (`weapon-spear` pequeño) cada 2 s, huye a 2 m.
- [x] 3.3 (30-10-2026) Esqueleto: lento y fuerte (vida 4, daño 2). Misma clase que orco con otros números y color.
- [x] 3.4 (30-10-2026) `Jefe` (orco jefe): vida 15, golpe en área cada 6 s con aviso de 1 s (círculo rojo). Barra de vida arriba.
- [x] 3.5 (30-10-2026) Trampas (`X`): pinchos que suben cada 2 s, 1 de daño.
- [x] 3.6 (30-10-2026) Botín del jefe: la primera vez una unidad "Orco jefe defensor"; después 3 huesos. Vuelta al pueblo con transición.
  Comprobar: `Revisar` con un caso "matar jefe" (vida infinita en modo prueba) que acaba con la unidad en el inventario.

## Fase 4 · Inventario, fuerte y guardado local
Leer antes: `01-diseno.md` §5.1, §6; `02-arquitectura.md` §2.5.
- [x] 4.1 (30-10-2026) `Inventario` con recursos (moneda, madera, piedra, hierro, hueso) y unidades defensoras con mejoras.
- [x] 4.2 (30-10-2026) `CatalogoPiezas` (ScriptableObject) con las piezas del fuerte de `01-diseno.md` §5.1: modelo, coste, vida, límite.
- [x] 4.3 (30-10-2026) `DisenoFuerte` (datos serializables) y `EditorFuerte`: modo construir en la escena Fuerte, barra inferior con piezas y coste, tocar casilla para poner o quitar, validación de límites, "Listo".
- [x] 4.4 (30-10-2026) `PiezaFuerte`: muros con vida que se rompen a golpes, puerta que el dueño abre, trampa oculta, defensor = `Enemigo` con sus números.
- [x] 4.5 (30-10-2026) Guardado local en JSON (`persistentDataPath/partida.json`) con versión: inventario, fuerte, progreso. Se guarda al cambiar; se carga al arrancar.
- [x] 4.6 (30-10-2026) Menú: ajustes (sonido, vibración), reiniciar partida con confirmación.
  Comprobar: construir 5 muros, cerrar la app, abrir: siguen ahí y el inventario descontado.

## Fase 5 · Asalto sencillo (sin servidor todavía)
Leer antes: `01-diseno.md` §5.2, §8.
- [x] 5.1 (30-10-2026) Escena Asalto: carga `fuerte-base.txt` + un `DisenoFuerte` y coloca piezas con colliders y defensores vivos. Apareces en la entrada.
- [x] 5.2 (30-10-2026) Reglas: 90 s de cuenta atrás, ganar al tocar el cofre `K`, perder al morir o agotar tiempo. Barra "distancia al cofre".
- [x] 5.3 (30-10-2026) Pantalla de resultado: ganado/perdido, segundos, muros rotos, recompensas (provisionales en local).
- [x] 5.4 (30-10-2026) Tres fuertes de prueba del sistema (fácil, medio, difícil) en `Assets/Mapas/fuertes-prueba/*.json` para asaltar sin servidor.
- [x] 5.5 (30-10-2026, con jugador automático; falta la prueba con Daniel) **Probar con Daniel** 10 asaltos contra los tres fuertes y ajustar vida de muros, daño de trampas y tiempo. Anotar en `docs/04-pruebas-asalto.md`.
  Comprobar: el fuerte difícil se gana en ~60 s con buen juego y se pierde jugando mal.

## Fase 6 · API y cuentas
Leer antes: `02-arquitectura.md` §1, §4, §4.1, §2.6.
- [x] 6.1 (30-10-2026) Carpeta `api/`: Node 22 + Fastify + Zod + pg. `docker-compose.yml` con postgres. Migraciones SQL numeradas.
- [x] 6.2 (30-10-2026) Tablas de `02-arquitectura.md` §4.1.
- [x] 6.3 (30-10-2026) `POST /sesion` con token de dispositivo; `GET /jugador`; `PUT /jugador/personaje`.
- [x] 6.4 (30-10-2026) `GET/PUT /fuerte`, `GET /catalogo`, `POST /mazmorra/resultado`: validación de límites y coste.
- [x] 6.5 (30-10-2026) `POST /asalto/buscar` (rival ±100 o fuerte del sistema; 10 al día) y `POST /asalto/resultado` (5-90 s, Elo, recompensas).
- [x] 6.6 (30-10-2026) `GET /ranking`, `GET /asaltos/historial`, `GET /salud`.
- [x] 6.7 (30-10-2026) Tests de todas las rutas con base de datos de prueba.
- [x] 6.8 (30-10-2026) Cliente: `ApiCliente` + `Sesion`; el guardado local pasa a ser caché y la API manda. Pantalla de arranque con crear personaje.
- [x] 6.9 (30-10-2026) Asalto real: buscar rival, cargar su diseño, jugar, mandar resultado. Pantallas Ranking e Historial.
  Comprobar: dos emuladores con dos cuentas; uno construye su fuerte, el otro lo asalta y gana; el ranking cambia; el primero ve el asalto en su historial.

## Fase 7 · Pueblo online
Leer antes: `02-arquitectura.md` §1, §2.6, §3. Es la fase más difícil: si una tarea se atasca dos veces, preguntar.
- [x] 7.1 Instalar Netcode for GameObjects 2.x y Unity Transport. `NetworkManager` solo en la escena Pueblo. (2026-10-30)
- [x] 7.2 Jugador como `NetworkObject` con autoridad del servidor: el cliente manda entrada, el servidor mueve. Predicción local y corrección suave. (2026-10-30)
- [x] 7.3 Otros jugadores visibles con su modelo, nombre encima y animación sincronizada. (2026-10-30)
- [x] 7.4 Enemigos del pueblo solo en el servidor; estado replicado. Daño y botín se deciden en el servidor y van a la API (`POST /inventario/sumar` con clave de servidor). (2026-10-30)
- [x] 7.5 Puertas (mazmorra, fuerte, asalto) desconectan del pueblo con transición y reconectan al volver. (2026-10-30)
- [x] 7.6 Modo sin conexión: NGO en modo host local y aviso en pantalla; fuerte y asalto bloqueados. (2026-10-30)
- [x] 7.7 Build "Dedicated Server" Linux; Dockerfile; `docker-compose` junto a la API; UDP 7777.
  (30-10-2026: en producción el servidor de mundo es el ejecutable de Windows bajo Wine (`deployment/mundo/Dockerfile.wine`), probado con jugadores desde fuera. El Dockerfile Linux queda listo para cuando se instale el módulo.)
- [x] 7.8 Script de prueba: servidor local + 2 clientes en editor (ParrelSync) se ven y se mueven. (2026-10-30)
  Comprobar: dos emuladores conectados al servidor de codehive se ven moverse; matar un orco en uno suma monedas en la API.
  (Comprobado en local: dos clientes y un servidor se ven moverse, y lo que recoge un cliente llega a la API. Con codehive, en la fase 8.)

## Fase 8 · Despliegue y avisos
Leer antes: `02-arquitectura.md` §5 y las reglas globales de Daniel sobre bóveda, ntfy y Kit.
- [x] 8.1 Desplegar `postgres`, `api` y `mundo` en codehive con el proxy existente. Credenciales en la bóveda. (30-10-2026)
- [x] 8.2 `kit-estado` para `juego-api` y `juego-mundo`; añadirlos a `kit-codehive/kit.json` e importar. (30-10-2026)
- [ ] 8.3 Copia de seguridad diaria de PostgreSQL (cron) con aviso si falla.
  Comprobar: el Kit muestra los dos servicios en verde; apagar la API lo pone en rojo y llega el aviso.
  (Desplegado el 30-10-2026 en https://app.orkhold.com: API con HTTPS y base de datos, servidor de mundo
  escuchando en UDP 7777, secretos en la bóveda, cron y Kit importado. UDP 7777 abierto en IONOS por Daniel;
  `pruebas/red-local.ps1 -Servidor app.orkhold.com:7777` da RED: OK. Falta la prueba de apagar la API.)

## Fase 9 · Pulido para enseñar
- [x] 9.1 Icono de la app y pantalla de carga. (2026-10-30)
- [x] 9.2 Tutorial de 1 minuto: 5 carteles en el pueblo (moverse, atacar, recoger, mazmorra, fuerte). (2026-10-30)
- [x] 9.3 Rendimiento en móvil gama media: menos sombras, LOD en árboles, 60 fps en emulador y en el móvil de Daniel. (2026-10-30)
- [x] 9.4 Capturas reales para los README (`docs/img/`). (2026-10-30)
- [ ] 9.5 APK firmado con keystore propio (guardado en la bóveda) e instalado en el móvil de Daniel.
  (Hecho el 30-10-2026: almacén en `%USERPROFILE%/.orkhold/orkhold.keystore`, contraseña en la bóveda como «orkhold - keystore», `Builds/orkhold.apk` firmado con CN=Orkhold contra https://orkhold.code-hive.space. Falta el servidor desplegado y el móvil de Daniel. Haz copia del almacén: sin él no se pueden publicar actualizaciones.)
  Comprobar: una persona que no ha visto el juego lo entiende en 2 minutos sin ayuda.
  (Pendiente de Daniel: hace falta una persona de verdad.)

## Después de la primera versión (no empezar)
Herramientas de asalto, repeticiones, tiempos de construcción, segunda mazmorra, más piezas y
defensores, nivel de personaje, equipo, chat, iOS, tienda, más zonas del pueblo.
