# 06 · Decisiones tomadas al implementar

Daniel pidió (29-10-2026) implementar todas las fases seguidas y tomar las decisiones sin parar.
Aquí queda cada una, con el porqué, para que se puedan revisar y cambiar. Lo más reciente abajo.

## Fase 1
| # | Decisión | Por qué |
|---|---|---|
| 1 | Se importa el Nature Kit completo (329 modelos) y los 12 personajes de Mini Characters. | Río, puente, vallas, cartel y personajes se repiten en todos los mapas. |
| 2 | Letras nuevas: `v` valla y `,` casilla reservada del hueco del fuerte. | La valla del fuerte no tenía letra; el hueco necesita casillas sin hierba para pintar el fuerte encima. |
| 3 | `[` es la esquina noroeste y `]` la sureste del hueco; dentro se pinta `fuerte-base.txt`. | Así el jugador ve su fuerte en el pueblo con su valla y su entrada. |
| 4 | `E` no tiene modelo: es solo la puerta de salida (mazmorra) o de entrada (fuerte). | Un mismo modelo no sirve para la salida de la mazmorra y el hueco de la valla. |
| 5 | Las puertas se cruzan andando (`D`, `E`, entrada al fuerte); el cofre PvP `C` se usa con el botón de interactuar. | El asalto es una decisión, no un paso. |
| 6 | Zona de puerta cuadrada de 0,8 m; hay que alejarse 1,4 m para que vuelva a funcionar. | Con zona redonda pequeña, el jugador se queda pegado al muro junto a la puerta sin entrar. |
| 7 | Casa 2x2 = 4 bloques `wall` con `wood-structure` encima y dos estandartes. | Mini Dungeon no trae casas; es lo que pide el diseño (`wall`, `wood-structure`). |
| 8 | Vallas 1,8 veces más altas que el modelo del Nature Kit. | El original mide 0,35 m, menos de la mitad del personaje. |
| 9 | Se mantienen los colores originales de Kenney (hierba turquesa). | Son los del pack; no es un fallo de importación. |
| 10 | Las escenas y las mallas fundidas no van en git; se montan desde los mapas al abrir Unity o al compilar. | Las mallas ocupan 35 MB de texto y cambian en cada montaje. |
| 11 | Todo lo fijo de un mapa se funde en mallas de 16x16 casillas; árboles y rocas del interior quedan sueltos; los árboles a menos de 3 casillas del borde se funden y no se pueden talar. | Rendimiento en móvil sin perder árboles y rocas que se golpean (fase 2). |
| 12 | Sombras duras a 18 m, sin cascadas; plano lejano de la cámara donde acaba la niebla. | Coste en móvil. |
| 13 | Emulador `Medium_Phone` con 4 GB de RAM (antes 2 GB; copia en `config.ini.original`). | Con 2 GB el emulador iba a 5-15 fps incluso con una escena vacía. |
| 14 | Fuerte y Asalto llevan una pradera grande alrededor. | Que no se vea el vacío al borde del fuerte. |
| 15 | Scripts, editor y tests en ensamblados propios (`Orkhold`, `Orkhold.Editor`, `Orkhold.Pruebas`). | Los tests no pueden ver el ensamblado por defecto de Unity. |

## Fase 2
| # | Decisión | Por qué |
|---|---|---|
| 16 | "Fuera de combate" = 5 s sin dar ni recibir golpes. | El diseño no lo define. |
| 17 | La fogata cura del todo al acercarse a 1,7 m, sin pulsar nada. | Es lo más rápido en móvil. |
| 18 | Al cambiar de escena el jugador entra con la vida llena. | El personaje se crea en cada escena; guardar la vida entre escenas no aporta en la primera versión. |
| 19 | El árbol o la roca agotada se encoge al 35 % durante 60 s y luego vuelve a crecer. | Se ve claro que está agotado sin modelos nuevos. |
| 20 | Al morir en la mazmorra se conserva la mitad de lo ganado redondeando a favor del jugador (de 5 se quedan 3). | El diseño dice "la mitad". |
| 21 | Al atacar, el personaje se gira solo hacia lo golpeable más cercano (1,6 m). | Apuntar con el joystick es difícil en móvil. |
| 22 | Modelos de los recursos en el suelo: madera = `log`, piedra = `stone_smallA`, hierro = `rocks` (Mini Dungeon), hueso = `stone_smallFlatB`. | No hay modelos de hierro ni de hueso en los packs. |
| 23 | Iconos del botón: mano (nada o recoger), llave (puerta), monedas (cofre PvP), estrella (tablón), llave inglesa (construir). | Son los iconos del pack Mobile Controls. |
| 24 | Sonidos: Kenney RPG Audio e Impact Sounds (CC0), en `Assets/Kenney/Audio/Resources/Sonidos`. | Los pide la tarea 2.6. |

## Fase 3
| # | Decisión | Por qué |
|---|---|---|
| 25 | El enemigo deja de perseguir si se aleja 9 m de su sitio o el jugador se va a más de 1,6 veces su distancia de visión, y vuelve andando. | Que no persigan por todo el mapa. |
| 26 | Solo los orcos del pueblo patrullan (3 casillas libres en línea recta, con 1,5 s de pausa en cada punta); los de la mazmorra esperan quietos. | El diseño pide patrulla de 2 puntos sin decir dónde. |
| 27 | Los enemigos de la mazmorra también reaparecen a los 60 s; el jefe no reaparece. | La regla de 60 s es general; el jefe cierra la entrada. |
| 28 | El arquero lleva la lanza en el brazo derecho y lanza otra lanza más pequeña (7 m/s); huye en la primera dirección libre. | El diseño dice `character-orc` + `weapon-spear`. |
| 29 | Esqueleto = `character-human` con la textura teñida de azul grisáceo. | Propuesta del diseño ("color de mazmorra"). |
| 30 | El jefe: golpe en área de radio 2,2 m; cuerpo a cuerpo 2 de daño cada 1,6 s; suelta 5 monedas además de su botín. | El diseño fija vida, daño y el intervalo de 6 s, no el radio ni la cadencia. |
| 31 | Al matar al jefe, la unidad o los 3 huesos van directos al inventario y a los 4 s se vuelve al pueblo. | Que el botín no se pierda si el jugador no lo recoge antes de la transición. |
| 32 | Trampas desfasadas según su posición (no suben todas a la vez); hacen daño una vez por subida. | Que la sala tenga un ritmo que se pueda leer. |

## Fase 4
| # | Decisión | Por qué |
|---|---|---|
| 33 | Coordenadas del diseño: x = columna de `fuerte-base.txt`, z = fila contada desde el sur. | Coinciden con las del mundo dentro del fuerte. |
| 34 | Las puertas cuentan para el límite de 40 muros. | Bloquean el paso como un muro. |
| 35 | No se construye en la casilla de llegada junto a la entrada (ahí aparece el atacante) ni sobre el cofre, la valla o la entrada. | Que el asalto siempre pueda empezar; los muros se rompen a golpes, así que no se exige camino libre (propuesta del diseño). |
| 36 | Tocar una casilla ocupada la quita y devuelve el coste entero (las mejoras con hueso no se devuelven). Los recursos se descuentan al poner, no al pulsar "Listo". | Lo más claro en móvil; "Listo" solo guarda. |
| 37 | Mejorar: herramienta "Mejorar" en la barra; cada toque sobre un defensor le da +1 vida por 2 huesos, hasta +3. | El diseño no dice dónde se mejora. |
| 38 | El orco jefe no cuesta recursos: gasta la unidad del botín y al quitarlo vuelve al inventario con sus mejoras. | Es una unidad única conseguida en la mazmorra. |
| 39 | En el fuerte propio la puerta se abre sola al acercarse el dueño; los defensores están quietos y las trampas se ven. En el asalto: puerta = muro de 4 de vida, trampas ocultas, defensores activos sin botín y sin reaparecer. | §5.1 y §5.2 del diseño. |
| 40 | Muro de madera = `fence_simpleHigh` centrado y 2,6 veces más alto; puerta = `gate` al 125-140 %. | Modelos del diseño, a la escala del personaje. |
| 41 | En el hueco del pueblo el fuerte se ve con sus piezas, sin colisiones ni defensores activos. | Solo es para verlo; se entra por la puerta a la escena Fuerte. |
| 42 | Guardado medio segundo después de cada cambio, al pausar y al salir; fichero roto → partida nueva y copia `.roto`. | No perder partida si el móvil cierra la app. |
| 43 | Ajustes (sonido, vibración) en PlayerPrefs, aparte de la partida; vibrar al recibir daño. | Reiniciar la partida no debe tocar los ajustes. |
| 44 | El menú no pausa el juego. | El pueblo será online (fase 7). |

## Fase 5
| # | Decisión | Por qué |
|---|---|---|
| 45 | Sin servidor, el cofre PvP abre un panel para elegir el fuerte de prueba (fácil, medio, difícil). | Poder probar los tres; la fase 6 lo cambia por la búsqueda de rival. |
| 46 | Salir por la entrada del fuerte durante el asalto = rendirse (cuenta como perdido). | El diseño no lo dice; evita salir y entrar para esquivar la derrota. |
| 47 | Recompensas y puntuación provisionales en local: ganar +10 monedas + 2 materiales al azar y +20 puntos; perder +3 monedas y -15 puntos. | Números del diseño; el Elo de verdad lo aplica la API. |
| 48 | Al terminar, todo se para y sale la pantalla de resultado con "Volver al pueblo". | Que el jugador lea el resultado. |
| 49 | Un golpe rompe como mucho una pieza del fuerte (la más cercana). | Un golpe rompía hasta tres muros. |
| 50 | **Cambio de diseño:** vida de muros madera 5, piedra 10, puerta 6 (antes 3, 6 y 4). | Con los números originales un asalto no llegaba a 20 s; ver `docs/04-pruebas-asalto.md`. |
| 51 | La tarea 5.5 se da por hecha con un jugador automático; la prueba con Daniel queda pendiente. | Daniel pidió seguir sin parar. |
| 52 | Reloj y barra del asalto a la derecha del centro; letra del HUD de 42 a 36. | Se solapaban con la segunda línea del HUD. |

## Fase 6
| # | Decisión | Por qué |
|---|---|---|
| 53 | Hasta el servidor de mundo (fase 7), el cliente manda el botín del pueblo a `POST /pueblo/botin`, con límite por minuto; se apaga con `PUEBLO_CLIENTE=0`. | Sin esto no se pueden conseguir madera ni piedra para el fuerte antes de la fase 7. |
| 54 | Unidades de botín (orco jefe) como recurso `orco_jefe` en la tabla `inventario`. | La tabla de §4.1 no tiene sitio para unidades. |
| 55 | Quitar un defensor devuelve también los huesos de sus mejoras (antes no). | La API cobra la diferencia entre diseños; así cliente y API coinciden. |
| 56 | Elo: ganar `40·(1−E)`, perder `−30·E`; fuertes del sistema con 900/1000/1100 puntos según la puntuación del atacante. | +20/−15 entre iguales como dice el diseño, "ajustado" según el rival. |
| 57 | Una derrota puede durar menos de 5 s (rendirse); la regla de 5-90 s es para victorias. Además, no se aceptan más segundos de los pasados desde la búsqueda. | Rendirse es legal; declarar tiempo falso no. |
| 58 | Personajes: 4 de Mini Characters (`male-a`, `female-a`, `male-c`, `female-c`), 12 % más grandes. | §6.1 pide 4; Mini Characters son algo más pequeños que el humano de Mini Dungeon. |
| 59 | Sin URL de API (modo local) el juego funciona como antes y así corren los tests. Con URL y sin conexión, se juega pero fuerte y asalto se bloquean. | §8 del diseño. |
| 60 | Puertos locales 3100 (API) y 5442 (PostgreSQL). | 3000 y 5432 los usa WSL en este PC. |
| 61 | Secretos de desarrollo en `api/.env` (fuera de git), generados al azar. | Regla de credenciales; los de producción irán a la bóveda (fase 8). |
| 62 | La prueba de "dos emuladores" se hizo con un emulador y dos cuentas seguidas (borrando los datos de la app entre una y otra); el historial de Ana se comprobó contra la API. | No hay disco: el segundo emulador pide 7,4 GB y quedan 5,4 GB. |
| 63 | Radio para "tocar el cofre": 1,25 m (antes 0,95). | En el emulador, pasando junto al cofre no contaba. |
| 64 | Al entrar en el fuerte el cliente pide a la API el inventario y el fuerte. | Construir con los datos más recientes. |
| 65 | En el asalto los defensores ven a 8 m y persiguen hasta 12 m; los del fuerte difícil del sistema llevan +3 de vida. | Equilibrio del asalto (`docs/04-pruebas-asalto.md`). |
| 66 | La URL de la API va vacía en git y se pone solo al compilar el APK (`-urlApi`); un test lo comprueba. | Un APK de prueba dejó la URL del emulador en el commit de la fase 6. |

## Fase 7
| # | Decisión | Por qué |
|---|---|---|
| 67 | Netcode for GameObjects 2.13.1 (la que trae Unity 6000.5) en vez de la 2.4.x fijada en `CLAUDE.md`. | La 2.4.x no compila con Unity 6000.5 (usa APIs que ahora son error). |
| 68 | El `NetworkManager` lo crea `RedPueblo` al cargar el pueblo y se destruye al salir por una puerta; sin gestión de escenas de Netcode. | Las otras escenas son de un solo jugador; así cada puerta desconecta y reconecta sin estado viejo. |
| 69 | Sin dirección de mundo, o si no conecta en 6 s, el pueblo arranca como anfitrión local en un puerto al azar entre 21000 y 29000, con aviso. | §8 del diseño; Windows reserva puertos sueltos fuera de ese rango. |
| 70 | Envío de entrada 30 veces por segundo y los toques al momento; corrección si la diferencia pasa de 0,35 m y salto si pasa de 3 m. | Valores habituales; con ellos el movimiento propio no tiembla. |
| 71 | Los orcos persiguen al jugador vivo más cercano (lo buscan cada 0,5 s). | Con varios jugadores, perseguir siempre al primero no tiene sentido. |
| 72 | En el servidor dedicado, cada jugador aparece con una separación al azar (hasta 0,6 m a los lados y 0,4 m adelante o atrás). | Si no, dos jugadores que entran a la vez aparecen uno encima del otro. |
| 73 | La prueba de red (7.8) usa el ejecutable de Windows con bots en vez de ParrelSync en el editor. | Se puede lanzar sin ventanas y sin abrir varios Unity; ParrelSync duplica el proyecto y no hay disco. |
| 74 | Con `-bot` las puertas no hacen nada. | Un bot que cruza una puerta sale del pueblo y estropea la prueba. |
| 75 | El servidor de mundo lee la URL de la API y la clave de servidor de variables de entorno (`ORKHOLD_API`, `ORKHOLD_CLAVE_SERVIDOR`). | La clave no puede ir en la compilación ni en git. |
| 76 | La API se publica en `127.0.0.1:4152` para el proxy de codehive, nunca directa a internet. | Que todo el tráfico pase por el proxy con HTTPS. |
| 77 | Al compilar el APK, `-mundoApk host:puerto` pone el servidor de mundo en la configuración y luego la deja vacía, igual que `-urlApi`. | En git la configuración va vacía (modo local); un test lo comprueba. |

## Fase 8
| # | Decisión | Por qué |
|---|---|---|
| 78 | Mientras falte el módulo de servidor Linux de Unity, el servidor de mundo corre en codehive con el ejecutable de Windows y Wine (`deployment/mundo/Dockerfile.wine`). `desplegar.sh` elige el Dockerfile de Linux en cuanto exista `Builds/Servidor/`. | El módulo pide permisos de administrador en el PC y no se pudo instalar. |
| 79 | El servidor de mundo escribe su estado (jugadores, hora) cada 30 s en un fichero montado; `orkhold-vigilar` (cron cada 5 min) lo lee y llama a `kit-estado`. | Desde dentro del contenedor no se puede llamar a `kit-estado`. |
| 80 | Proyecto `deployment_orkhold` en `/home/codehive/codehive-app-state/deployments/orkhold/workspace`, API en 127.0.0.1:4152 detrás de Traefik, como aldea y poker. | Mismo patrón que los despliegues recientes de codehive. |
| 81 | Los secretos de producción se generan en el servidor (`orkhold-secretos generar`) y se guardan en la bóveda con `orkhold-secretos guardar`. | Nadie los ve; la bóveda estaba bloqueada al preparar el despliegue. |
| 82 | La copia diaria (04:15, 14 días) avisa al móvil con `kit-avisar juego-api` si falla, sin cambiar el estado del servicio. | `orkhold-vigilar` pondría el estado otra vez en verde a los 5 minutos y el fallo se perdería. |
| 83 | En producción `PUEBLO_CLIENTE` va apagado: el botín del pueblo solo lo suma el servidor de mundo. | Con el servidor de mundo, el cliente no debe sumar nada por su cuenta (§3). |

## Fase 9
| # | Decisión | Por qué |
|---|---|---|
| 84 | Icono: el orco de Mini Dungeon de cuerpo entero sobre un degradado verde, generado por `CrearIcono`. Logo del arranque: el orco y el nombre; sin el logo de Unity. | El diseño no dice nada; el orco es lo más reconocible del juego. |
| 85 | Pantalla de carga: la pantalla negra de cada cambio de escena muestra el nombre y uno de 7 consejos al azar. | Ya existía el fundido; así no hay una escena de carga más. |
| 86 | Carteles del tutorial como letras `1`-`5` del mapa del pueblo, con su texto en la cabecera (`cartel1`...`cartel5`); el texto sale arriba en un recuadro a menos de 2,4 m. Los cinco están cerca de la fogata, en el camino natural. | Se editan en el mapa de texto como lo demás; un tutorial de 1 minuto no puede obligar a cruzar el mapa. |
| 87 | Rendimiento: el suelo y lo que mide menos de 0,4 m no proyectan sombra; árboles y rocas sueltos con LOD que los quita por debajo del 3 % de pantalla. Sombras duras a 18 m sin cambios. | Con la cámara fija desde arriba el LOD casi no actúa; lo que más ahorra es la pasada de sombras. Resultado: de 56-59 a 59-60 fps en el emulador. |
| 88 | Capturas del README: arranque, carga, pueblo, tutorial y mazmorra nuevas del emulador; construir y asalto, del emulador en la fase 6 (no han cambiado). | El asalto necesita dos cuentas y la API; las de la fase 6 son reales y actuales. |
| 89 | La firma del APK lee la contraseña del almacén de la bóveda por SSH y la pasa a Unity por el entorno; la ruta no se queda en ProjectSettings. El almacén vive en `%USERPROFILE%/.orkhold/`. | Regla de credenciales: nada en el repositorio ni en ficheros sueltos con la contraseña. |
| 90 | El color cian del césped no se toca. | Es como se ven los materiales del pack de naturaleza desde la fase 0; cambiarlo es una decisión de aspecto para Daniel. |
| 91 | Dominio: `app.orkhold.com` para la API y el servidor de mundo; `orkhold.com` queda libre para la landing. | Lo decidió Daniel el 30-10-2026; sustituye a `orkhold.code-hive.space`. |
| 92 | La red de Docker de Orkhold usa la subred fija 10.96.0.0/24. | En codehive Docker ya no tiene subredes libres; otros proyectos usan 10.90-10.95. |
| 93 | El servidor de mundo bajo Wine arranca Xvfb a mano, con `init: true`, y escribe su registro en `estado/mundo.log`. | Sin pantalla Wine no arranca; `xvfb-run` como primer proceso se queda colgado; con `-logFile -` el juego no arranca. |
| 94 | La tarea 7.7 se cierra con el servidor de mundo bajo Wine en producción. | Funciona desde fuera (RED: OK contra app.orkhold.com); el módulo Linux de Unity no se ha podido instalar sin permisos. |

## Versión 2
| # | Decisión | Por qué |
|---|---|---|
| 95 | La versión 2 empieza sin esperar las comprobaciones de la 1 que dependen de Daniel (móvil, persona real, apagar la API). | Daniel lo aprobó todo el 30-10-2026 y pidió implementarlo. |
| 96 | Orden: balance y panel (10), cuentas (11), saqueo (12), lore (13). Mazmorras 2-4, eventos y social esperan a sus documentos de diseño. | El orden de `09-plan-version-2.md`; el lore es pequeño e independiente. |
| 97 | El panel de administración lo sirve la API en `app.orkhold.com/admin/`, con Vue desde CDN y sin compilar. | Sin otro dominio ni contenedor; el panel es pequeño. |
| 98 | Los correos de cuenta salen por SMTP configurable; sin configurar, el enlace va al registro de la API. Sin verificar se juega igual. | No hay buzón de orkhold.com todavía; no bloquear a nadie por eso. |
| 99 | Lo ganado se guarda en los escondites en cuanto llega a la API, no al tocar la Brasa. | Mismo efecto para el jugador (lo que llevas encima no se puede saquear) y una sola regla en el servidor. |
| 100 | Orden de depósito: cofre central y luego los escondites en el orden en que se construyeron. Al gastar se saca en el mismo orden. | "Primer escondite con hueco" (08 §3.1); gastar del central primero lo vacía, que es lo más expuesto. |
| 101 | El fuerte de prueba (sin rivales) no mueve botín, ni tasa, ni Elo. | 08 §4.1: "sin botín real ni tasa"; dar Elo contra un fuerte falso inflaría el ranking. |
| 102 | "Mirar" es una petición al servidor; el móvil no conoce el contenido de los escondites del rival. | El servidor decide (08 §4.4) y así no se puede leer el contenido desde el cliente. |
| 103 | El balance guarda números (no textos) con mínimo y máximo; los costes son claves `<cosa>.coste.<recurso>` y solo existen las que tienen valor de partida. | Un panel con rangos evita errores al teclear; añadir un recurso nuevo a un coste se hace en `balance.json`. |
| 104 | Clave extra `saqueo.velocidad_max` (5,5 m/s) para rehacer la exploración. | Correr va a 4,5 m/s; el servidor necesita un tope con margen y es un número de balance. |
| 105 | Jefe de la mazmorra: la unidad la primera vez; hueso y hierro la primera vez de cada día (UTC); monedas las demás. | 08 §1.2; antes daba 3 huesos siempre. |
| 106 | Administradores aparte de los jugadores (tabla `administradores`), con token firmado con otra clave y 12 h. | Un token de jugador nunca vale para el panel. |
| 107 | El servidor de mundo manda un latido con los jugadores del pueblo a la API cada 30 s. | Así el panel sabe cuántos hay sin leer ficheros del servidor. |
| 108 | Versión del juego como número fijo en `Configuracion.VersionJuego` (empieza en 2); el panel pone la mínima. | Hace falta un número que compare la API; se sube con cada APK publicado. |
| 109 | Árboles y rocas del pueblo reaparecen a los 300 s (antes 60) y lo marca el balance. | 08 §1.2 aprobado: "Árboles y rocas: cada 5 min". |
| 110 | Una invitada registrada ya no entra con el token del dispositivo (`/sesion` responde `cuenta_registrada`); el móvil guarda su token de sesión de 30 días. | 14-cuentas: la cuenta no va atada al móvil; si alguien copia el token del dispositivo no entra en una cuenta con correo. |
| 111 | Con servidor, el menú enseña "Cuenta" en vez de "Reiniciar partida". | Reiniciar solo tenía sentido con la partida guardada en el móvil. |
| 112 | Lista de nombres prohibidos corta y solo con palabras que no forman parte de nombres normales. | Evitar rechazar nombres legítimos; la lista se amplía en `api/src/datos/nombres-prohibidos.txt`. |
| 113 | `PUT /fuerte` reparte solo el contenido de los escondites (los que siguen en su casilla conservan lo suyo); `PUT /fuerte/escondites` es para recolocar a mano. | El jugador construye sin hacer cuentas; recolocar es una acción aparte (08 §3.1). |
| 114 | El contenido de antes de la versión 2 pasa entero al cofre central aunque pase de 50; no entra más hasta que haya sitio. | No quitar nada a nadie al cambiar de modelo. |
| 115 | Solo sale como rival quien tiene algo guardado; si no hay nadie, fuerte de prueba. | Saquear un fuerte vacío no enseña nada y cuesta la tasa. |
| 116 | Las marcas necesitan camino libre desde la entrada (con lo roto), no que el atacante haya pisado al lado; las miradas sí tienen que hacerse a 1,8 casillas como mucho. | 08 §4.3 habla de camino; mirar es acercarse. |
| 117 | El servidor rehace la exploración revisando cada cuarto de casilla del recorrido, con un golpe cada 0,5 s como mucho y velocidad máxima del balance. | Sin eso, un salto rápido atravesaría un muro; el ataque dura 0,6 s. |
| 118 | El botín se calcula contra el fuerte del defensor tal como está al resolver; el muro roto y el camino, con el fuerte que vio el atacante al empezar. | El servidor decide con lo que hay de verdad; si el defensor recoloca mientras, le sirve. |
| 119 | Fuerte de prueba: el del sistema con 2 cofres, 2 barriles y 2 enterrados al azar (la mitad vacíos) y 12 monedas en el central. | Para practicar mirar y marcar aunque no haya rivales. |
| 120 | El cofre, el barril y el cofre central no se pueden pisar; el enterrado, sí. | Son piezas con cuerpo; el enterrado parece suelo. |
| 121 | Herramientas como unidades del inventario (`farol`, `cuarta_accion`...), fabricadas con `POST /herramientas/fabricar`. | Ferra las fabrica antes; se gastan al empezar el saqueo (08 §5). |
| 122 | Las rutas del asalto de 90 s se quitan; la tabla `asaltos` se queda como historia. La ruta pública conserva el campo `asaltos` (ahora cuenta saqueos) para la web. | Un solo PvP; la web de orkhold.com ya lee ese campo. |
