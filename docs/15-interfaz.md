# 15 · Interfaz y sensación de juego

Fecha: 2026-10-30. Petición de Daniel: "la UX es un poco tosca, me falta un HUD bonito, que se vea mejor todo,
que las interacciones se entiendan mejor; no me gusta que al acercarnos salga el texto del personaje directamente;
encima de los personajes debería poner su nombre; esos pequeños detalles de los MMORPG que te atrapan".
**Propuesta** del modelo. Preguntas para Daniel en §9.

## 1. Lo que hoy se ve tosco
| Qué | Hoy |
|---|---|
| HUD | Dos líneas de texto arriba a la izquierda ("Vida 6/6 · Monedas 0 · Madera 0..."). |
| Letra | Arial de Unity en todo. |
| Personajes del pueblo | Al acercarte, su texto sale solo en un recuadro arriba. No se ve su nombre. |
| Nombres | Solo los otros jugadores llevan nombre, en texto plano. Ni tú, ni los NPC, ni los enemigos. |
| Interactuar | El botón de la mano no dice qué va a hacer. No se marca qué objeto vas a usar. |
| Avisos | Todos iguales: texto blanco en el centro. |
| Recoger, golpear | Números flotantes y poco más. |

## 2. Estilo
- **Material gráfico** (gratis, CC0, de Kenney, como los modelos):
  - *UI Pack RPG Expansion*: paneles de madera y pergamino, barras, botones.
  - *Game Icons* (o *Board Game Icons*): iconos de moneda, madera, piedra, hierro, hueso, poción, espada, mano.
  - *Kenney Fonts*: letra para títulos y nombres (la "Kenney Future" o similar) y otra legible para textos.
- **Texto con TextMeshPro** (ya viene en Unity): contorno y sombra nítidos a cualquier tamaño, también sobre las cabezas.
- **Colores**: los de la marca (verde 155 y brasa 25) para lo importante; dorado para el botín; rojo solo para daño y peligro.

## 3. HUD nuevo
```
┌──────────────────────────────────────────────────────────────────────────┐
│ (retrato) Nombre          ●Vado Gris●              🪙 128   [≡]          │
│  ♥♥♥♥♥♡  ▓▓▓▓▓░ 37/80     (aparece al entrar         🪵12 🪨4 ⛓3 🦴6       │
│          escondites        y se va en 2 s)                                  │
│                                                                            │
│                                                                            │
│  ( joystick )                                     [🧪3]  [Hablar ✋]        │
│                                                          [⚔]   [»]        │
└──────────────────────────────────────────────────────────────────────────┘
```
- **Arriba a la izquierda**: retrato de tu personaje en un marco, tu nombre, la vida en corazones y una barra
  pequeña con lo guardado en tus escondites (verde; ámbar al 80 %; roja llena, con "Escondites llenos").
- **Arriba en el centro**: el nombre del sitio al entrar ("Vado Gris", "La Forja Hundida"), en un cartel que baja,
  se queda 2 s y se va.
- **Arriba a la derecha**: monedas grandes y, debajo, los demás recursos con su icono. Al recoger algo, el icono
  vuela desde el objeto hasta su contador y el número da un pequeño salto.
- **Abajo a la derecha**: el botón de interactuar dice qué hará ("Hablar", "Recoger", "Entrar", "Mirar",
  "Saquear", "Construir"); botón de poción con su número (hoy la poción se usa sin botón propio).
- Avisos de tres tipos: **botín** (dorado, abajo, apilables "+3 madera"), **información** (cartel arriba) y
  **peligro** (rojo, con borde y sonido).

## 4. Nombres sobre las cabezas
| Quién | Cómo |
|---|---|
| Tú y otros jugadores | Nombre en blanco con contorno; debajo, el estandarte de temporada como una franja de su color. |
| Personajes del pueblo | Nombre en dorado y su oficio debajo, más pequeño: "Ferra · herrera". |
| Enemigos | Nombre en rojo y barra de vida pequeña, solo cuando estás cerca o les pegas. |
| Jefes | Como ahora (barra grande arriba), con su nombre en la letra de títulos. |
Los nombres se ven de lejos y se ocultan si hay demasiados a la vez (máximo 12 en pantalla, los más cercanos).

## 5. Hablar con los personajes del pueblo
- **Nada salta solo.** Encima de cada personaje hay un icono:
  - "!" amarillo: tiene algo nuevo que contarte (la primera vez, el tutorial, algo nuevo que fabricar).
  - Su icono de oficio en gris: ya hablaste; puedes volver.
- Al acercarte, el personaje te mira y el botón dice **"Hablar"**.
- Al hablar, **caja de diálogo** abajo: retrato, nombre, el texto letra a letra (tocar lo completa) y botones:
  | Personaje | Opciones |
  |---|---|
  | Maela | "Cuéntame" (tutorial, por partes) · "Adiós" |
  | Rook | "¿Cómo se pelea?" · "Mi parcela" (lleva a construir) · "Adiós" |
  | Ferra | "Fabricar" (su tienda) · "Adiós" |
  | Ansel | "¿Qué hay al norte?" (qué mazmorras están abiertas) · "Adiós" |
  | Tobin | "Clasificación" · "Mis saqueos" · "La Ley del Vado" · "Adiós" |
  | Gruk | "¿Qué vendes?" · "Adiós" |
- El tutorial deja de ser cinco carteles: Maela lo cuenta paso a paso y te manda a hablar con los demás.

## 6. Que se entienda qué se puede usar
- **Lo que vas a usar se marca**: un aro en el suelo y su icono flotando encima (mano, puerta, cofre, bocadillo).
- **Puertas**: al acercarte, un cartel con el destino: "Las catacumbas", "La Forja Hundida · vence a Urzog".
- **Recogibles**: brillan un poco y saltan hacia ti al recogerlos.
- **Árboles y rocas agotados**: se ven apagados y con un reloj pequeño mientras vuelven.

## 7. Sensación al jugar
- Golpe: pequeño retroceso, destello blanco en el enemigo y sacudida corta de cámara solo en golpes fuertes.
- Muerte de enemigo: el botín sale disparado y cae con un pequeño rebote.
- Jefe vencido: cámara lenta medio segundo, fanfarria y cartel "¡Grimbra vencida!".
- Sonidos de interfaz (abrir panel, comprar, botín, error), del pack de audio de Kenney que ya está en el proyecto.

## 8. Lo que engancha (cada uno es una decisión)
1. **Encargos diarios de Tobin**: 3 al día ("vence a 10 orcos", "tala 15 árboles", "saquea un fuerte"),
   con monedas y algún material. Se renuevan a las 6:00 como los saqueos. Se ven en el HUD (icono de pergamino con contador).
2. **Racha de días**: entrar días seguidos da un pequeño premio que crece hasta el día 7.
3. **Logros y títulos**: "Primer saqueo", "Vencedor de Urzog", "Escondedor experto"... Un título elegido se ve
   bajo tu nombre. Sin niveles numéricos (08 §1.4).
4. **"Mientras no estabas"**: al entrar, un resumen corto: saqueos recibidos, botín perdido o ganado, posición en la
   temporada, encargos nuevos.
5. **Avisos al móvil** (notificaciones de Android): "Te han saqueado", "Tus saqueos se han recargado". Necesita
   Firebase o similar: más trabajo y un servicio externo.

## 9. Preguntas para Daniel
1. ¿Descargas tú tres packs de Kenney como la otra vez (UI Pack RPG Expansion, Game Icons y Kenney Fonts)?
   *Propuesta:* sí; sin ellos el HUD se puede mejorar, pero seguirá pareciendo hecho con cajas.
2. ¿Qué entra de §8? *Propuesta:* 1, 3 y 4 ahora; 2 después; 5 al final, con Play Store.
3. Orden. *Propuesta:* una fase de interfaz (§2-§7) antes de seguir con las mazmorras 3 y 4, y otra fase para §8.

## 10. Aspecto del mundo (aclaración de Daniel, 30-10-2026)
Daniel no pide funciones nuevas: quiere que **se vea** con ganas de jugar. Hoy se ve soso, con mucho espacio vacío y
colores pastel demasiado alegres para un valle de orcos. El pueblo parece "una zona plana con dos recuadros" y la
mazmorra "cuatro piedras". Los encargos, rachas y logros de §8 quedan fuera por ahora.

### 10.1 Hecho como prueba (30-10-2026)
- Luz de tarde (sol bajo y cálido), ambiente y niebla grises en vez de azul cielo.
- Materiales de los packs copiados más oscuros y menos saturados al montar las escenas (`EstiloVisual`);
  hierba verde oliva, caminos de tierra, agua oscura. Personajes, enemigos y botín conservan su color.
- Árboles de la variante oscura del Nature Kit y el triple de relleno pequeño (hierba alta, arbustos, rocas, tocones).
- Antes y después: `Revision/antes/` y `Revision/`.

### 10.2 Pueblo de Vado Gris (por hacer)
| Qué | Cómo |
|---|---|
| Que parezca un pueblo | Empalizada de madera alrededor con dos puertas, calles empedradas (Graveyard `road`), casas con tejado y puerta, no bloques. |
| La plaza | Plaza empedrada en el centro con **la Brasa** de verdad: hoguera grande con llamas, chispas, humo y luz cálida que parpadea. Bancos, el tablón de Tobin, faroles. |
| Oficios | La forja de Ferra con yunque, barriles y fuego; el puesto de Gruk con estandartes orcos; la mesa de mapas de Ansel. |
| Ruinas del norte | La Orkhold caída con torres rotas del Castle Kit, columnas y las puertas de las mazmorras entre ellas. |
| Tierras del Este | Vallas, caminos y estandartes en la entrada de la parcela. |
| Vida | Humo de chimeneas, pájaros, polillas cerca de los faroles y orcos patrullando fuera de la empalizada (no junto a las puertas). |

### 10.3 Mazmorras (por hacer)
| Qué | Cómo |
|---|---|
| Catacumbas | Ataúdes, criptas, lápidas, velas, urnas y huesos del Graveyard Kit; suelo con losas distintas; telarañas de luz verde. |
| Forja Hundida | Braseros con fuego vivo, yunques, cadenas, barriles y luz roja de las trampas de fuego. |
| Todas | Antorchas en las paredes que iluminan a trozos, oscuridad en las esquinas, polvo flotando y paredes con columnas y variaciones, no un muro repetido. |

### 10.4 Interfaz (§2-§7)
Con los packs ya descargados (UI Pack RPG Expansion, Game Icons y Kenney Fonts): HUD, nombres, diálogos y avisos de §3-§6.
