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
