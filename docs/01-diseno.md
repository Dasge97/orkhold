# 01 · Diseño del juego

Nombre: **Orkhold** (elegido el 29-10-2026; dominios .com/.app/.games/.io/.es libres ese día, sin app ni juego con ese nombre).
Fecha: 2026-10-29. Decisiones tomadas por Daniel; lo marcado como *propuesta* lo puede cambiar.

## 1. Idea en una frase

Un mundo online en 3D low-poly para móvil, visto desde arriba, con un **mapa general** estilo
ciudad que irá creciendo, **mazmorras** que son mapas aparte (los niveles), y un **fuerte** propio
que construyes con lo recolectado y que otros jugadores asaltan (el PvP).

## 2. Decisiones cerradas

| Tema | Decisión | Por qué |
|---|---|---|
| Plataforma | Android primero, iOS después | Daniel tiene Unity 6 y emulador Android. |
| Motor | Unity 6 (6000.5.9f1), render built-in, entrada clásica | Ya instalado, prototipo hecho. |
| Gráficos | Packs 3D de Kenney (CC0): Mini Dungeon, Mini Characters, Nature Kit, Mobile Controls | Mismo estilo, misma escala, sin dibujar. |
| Controles | Pantalla completa, joystick a la izquierda, 3 botones a la derecha | Como una consola, pedido por Daniel. |
| Mapa general | Un mapa mediano compartido, estilo ciudad, al que se van añadiendo cosas con el tiempo | Es el centro de todo: ves a otros jugadores, entras a mazmorras, vas a tu fuerte. |
| Niveles | Las mazmorras. Cada una es un mapa suelto: al entrar hay una transición y cambias de mundo, como pasar a la batalla en Clash of Clans | Cada mazmorra: jefe propio, recompensa propia, más difícil que la anterior. |
| Fuerte | En un lateral del mapa general hay un hueco donde cada jugador ve **su** fuerte. Lo construye con muros, puertas, trampas y unidades defensoras. Los demás solo lo ven al asaltarlo | Es lo que el PvE alimenta y lo que el PvP ataca. |
| PvP | Asaltar el fuerte de otro jugador. Transición a una copia de su fuerte, como en Clash. En la primera versión: **muy sencillo**. No se despliegan unidades; la idea a futuro son "herramientas" para destruir. El detalle queda abierto a propósito | Daniel no quiere cerrar el PvP todavía. |
| Online | Todo online: ves a otros jugadores en el mapa general | Pedido por Daniel. Implica servidor en tiempo real desde la primera versión. |
| Primera versión | Pequeña y completa: mapa general, 1 mazmorra con jefe, 3 enemigos, fuerte construible, asalto sencillo real contra otro jugador, guardado | Mejor algo cerrado que mucho a medias. |

## 3. Bucle de juego

```
Mapa general (online, compartido)
   │ recolectas, matas enemigos sueltos, ves a otros
   ├──► puerta ──transición──► MAZMORRA (mapa aparte, solo tú) ──► jefe ──► botín ──► vuelves
   ├──► lateral ──► TU FUERTE (solo lo ves tú) ──► construyes muros, trampas, defensores con lo recolectado
   └──► cofre PvP ──transición──► ASALTO al fuerte de otro ──► resultado ──► recompensas ──► vuelves
```

Una sesión de 5 minutos tiene que dar algo: un material nuevo, un muro más, un asalto.

## 4. El mapa general

### 4.1 Zona inicial: el pueblo (primera versión)
- Rejilla de 48x48 casillas de 1 m. Plaza central con fogata (aparición), cofre PvP y tablón de ranking.
  Calles de tierra, casas (de Mini Dungeon: `wall`, `wood-structure`), río con puente, bosque en los bordes.
- **Puerta de mazmorra** al norte. Más adelante habrá más puertas: cada mazmorra nueva es una puerta nueva.
- **Hueco del fuerte** al este: un terreno de 16x16 vallado con una entrada. Cada jugador ve su propio fuerte ahí.
- Enemigos sueltos por el mapa que reaparecen cada 60 s en su sitio.
- El mapa se escribe en **ficheros de texto** (ver `02-arquitectura.md` §2.2). Añadir cosas al pueblo es editar el fichero.

### 4.2 Enemigos de la primera versión (3 tipos)
| Nombre | Modelo | Vida | Daño | Comportamiento | Suelta |
|---|---|---|---|---|---|
| Orco | `character-orc` | 3 | 1 | Persigue a 5 m, pega a 1 m cada 1,3 s | 1-2 monedas |
| Orco arquero (*propuesta*) | `character-orc` + `weapon-spear` | 2 | 1 | Se queda a 4 m y lanza cada 2 s; huye si te acercas | 1 moneda + 20 % hierro |
| Esqueleto (*propuesta*: `character-human` con color de mazmorra) | 4 | 2 | Lento (1,5 m/s), pega fuerte | 1 moneda + 30 % hueso |

### 4.3 Mazmorra 1: las catacumbas (mapa aparte)
- Al tocar la puerta: fundido a negro 0,5 s, se carga la escena de la mazmorra, fundido de vuelta.
  La mazmorra es **solo tuya** (instancia por jugador); al salir o morir vuelves a la puerta del pueblo.
- Sala de entrada, pasillo, sala de trampas (`trap`), sala del jefe. Diseñada en texto, 20x30 casillas.
- Jefe: **Orco jefe**. Modelo `character-orc` al 150 %. Vida 15, daño 2. Cada 6 s golpe en área (hay que alejarse).
- Botín fijo al matar al jefe la primera vez: una **unidad defensora** (ver §5.1). Después: materiales.
- Al morir dentro, vuelves al pueblo con la mitad de los materiales recogidos en esa entrada.

### 4.4 Recolección
| Recurso | De dónde | Para qué |
|---|---|---|
| Monedas | Enemigos, cofres | Comprar unidades defensoras y trampas |
| Madera | Árboles del pueblo (golpear 3 veces) | Muros y puertas de madera |
| Piedra (*propuesta*) | Rocas del pueblo (golpear 3 veces) | Muros de piedra (más vida) |
| Hierro | Orcos arqueros, mazmorra | Trampas |
| Hueso | Esqueletos, mazmorra | Mejorar unidades defensoras |

### 4.5 Jugador
- Vida 6. Recupera 1 cada 10 s fuera de combate y del todo en la fogata.
- Acciones: mover, atacar (1 de daño, alcance 0,9 m; también rompe muros en el asalto), interactuar
  (recoger, abrir cofre, tablón, puerta, construir en el fuerte), correr (mantenido).
- Sin niveles de personaje en la primera versión. El progreso es el fuerte.

## 5. El fuerte

### 5.1 Qué se construye
Rejilla de 16x16 casillas. En el centro, fijo, el **cofre del tesoro** (lo que el atacante quiere).
Desde el modo construir (botón al entrar al fuerte) se coloca en casillas libres:

| Pieza | Modelo | Cuesta | Vida | Qué hace |
|---|---|---|---|---|
| Muro de madera | `fence_simpleHigh` | 2 madera | 5 | Bloquea el paso |
| Muro de piedra | `wall` | 3 piedra | 10 | Bloquea el paso |
| Puerta | `gate` | 2 madera + 1 hierro | 6 | Solo la abre el dueño; el atacante la rompe |
| Trampa de pinchos | `trap` | 2 hierro | — | Oculta; 1 de daño al pisarla, cada 2 s |
| Orco defensor | `character-orc` | 10 monedas | 3 | Persigue y pega dentro del fuerte |
| Esqueleto defensor | `character-human` oscuro | 15 monedas | 4 | Lento y fuerte |
| Orco jefe defensor | botín de la mazmorra 1 | — | 15 | Golpe en área |

Vidas de muros y puerta subidas el 30-10-2026 (antes 3, 6 y 4) tras las pruebas de asalto: con 3 y 6 golpes un
asalto no pasaba de 20 s (`docs/04-pruebas-asalto.md`).

Límites de la primera versión: 40 muros, 6 trampas, 4 defensores. Mejora con hueso: +1 vida por 2 huesos, máximo +3.
Construir es inmediato (sin tiempos de espera en la primera versión).

### 5.2 Asalto (primera versión, muy sencillo)
- Desde el cofre PvP del pueblo: "Buscar rival". El servidor elige un jugador de puntuación parecida
  y manda el **diseño guardado** de su fuerte. Transición, y apareces en la entrada de una copia.
- Juegas **con tu personaje y los mismos controles del PvE**: rompes muros a golpes (cada golpe 1 de vida al muro), esquivas trampas, luchas contra sus defensores.
- Ganas si **tocas el cofre del tesoro** antes de 90 s y sin morir. Pierdes si mueres o se acaba el tiempo.
- El defensor no tiene que estar conectado y no pierde nada al ser asaltado; recibe un aviso con el resultado y puede ver la repetición más adelante (fuera de la primera versión).
- Puntuación tipo Elo desde 1000. Ganar +20 ajustado, perder -15. Recompensa: ganar 10 monedas + 2 materiales al azar; perder 3 monedas. Máximo 10 asaltos al día (*propuesta*).
- **Lo que queda abierto** (a decidir más adelante, no en la primera versión): herramientas de asalto (pico, bomba, antorcha, escalera), dos atacantes a la vez, repeticiones.

## 6. Pantallas

1. **Arranque**: logotipo, conexión, crear personaje (4 modelos de Mini Characters y un nombre).
2. **Pueblo** (principal): juego con joystick, botones, HUD arriba a la izquierda (vida, monedas, materiales), botón de menú arriba a la derecha.
3. **Fuerte**: al entrar al hueco aparece el botón "Construir". En modo construir: barra inferior con piezas y su coste, se toca una casilla para poner o quitar. Botón "Listo" guarda en el servidor.
4. **Asalto**: igual que el pueblo más una cuenta atrás de 90 s y una barra "distancia al cofre". Al terminar: pantalla de resultado y recompensas.
5. **Ranking** (tablón): los 50 mejores y tu puesto. **Historial**: últimos 10 asaltos hechos y recibidos.
6. **Ajustes** (menú): sonido, vibración, cerrar sesión.

## 7. Lo que NO entra en la primera versión
Herramientas de asalto, desplegar unidades, repeticiones, tiempos de construcción, chat, gremios,
misiones, tienda con dinero real, iOS, segunda mazmorra, nivel de personaje, equipo del personaje, clima.

## 8. Riesgos
- **Todo online** es lo más caro. Servidor caído = nadie juega. Mitigación: el pueblo y la mazmorra
  funcionan en modo "solo" sin conexión; se bloquea el asalto, el fuerte y el guardado hasta reconectar.
- El asalto se juega en el móvil del atacante, así que el resultado lo manda el cliente y se puede
  falsear. Mitigación en la primera versión: el servidor comprueba que el asalto duró entre 5 y 90 s y
  limita a 10 al día. Más adelante: simular el asalto también en el servidor.
- Un fuerte sin salida posible (todo muros) sería injusto. Mitigación: el modo construir exige un
  camino libre hasta el cofre... o no, y los muros se rompen a golpes (3 golpes madera, 6 piedra):
  siempre hay camino, solo cuesta tiempo (5 golpes madera y 10 piedra desde el 30-10-2026). *Propuesta*: la segunda.
- Kenney tiene pocos personajes distintos. Mitigación: variar color y tamaño del mismo modelo.
