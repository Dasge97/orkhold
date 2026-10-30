# 11 · Mazmorras 2, 3 y 4

Fecha: 2026-10-30. Bloque 3 de `09-plan-version-2.md`. **Aprobado por Daniel el 30-10-2026**, con las seis
propuestas de §7 tal cual (packs Graveyard Kit y Castle Kit, Torre en tres plantas, Pozo por oleadas,
defensores y trampas del saqueo para todos con la mazmorra 4, una mazmorra por fase, mapa de Ansel después).

## 1. Reglas comunes

| Tema | Regla (igual que la mazmorra 1 salvo que se diga otra cosa) |
|---|---|
| Acceso | Una puerta nueva en el pueblo por mazmorra, en las Ruinas de la Orkhold (norte). La mazmorra N se abre al vencer al jefe de la N-1; hasta entonces, la puerta dice "Ansel: todavía no he encontrado la entrada". |
| Instancia | Solo tuya, como las catacumbas. Morir: vuelves al pueblo y pierdes la mitad de lo ganado en esa entrada. |
| Jefe | La primera vez da su recompensa única; después, como Urzog: hueso y hierro la primera vez de cada día y monedas las demás (números en el balance, `mazmorra<N>.*`). |
| Dificultad | Cada mazmorra, enemigos con más vida y daño (tabla de §5). Sin niveles de personaje: se compensa con pociones y con conocer el mapa. |
| Botín máximo | Por entrada, en el balance (`mazmorra<N>.max_*`), como hoy en la mazmorra 1. |
| Desbloqueos | Cada jefe abre herramientas de saqueo y piezas del fuerte (§6). Se activan poniendo `disponible`/`limite` en el balance para quien tenga el jefe vencido. |
| Mapas | Ficheros de texto como `catacumbas.txt`, con las mismas letras. Los planos de este documento son borradores: se ajustan al montarlos. |

## 2. Mazmorra 2 — la Forja Hundida

**Lore.** Bajo las ruinas, la forja donde los orcos hacían sus armas. Se hundió cuando murió el rey y
los herreros siguieron trabajando a oscuras. Ferra quiere su hierro: "Tráeme lo de la Forja y te hago un pico".

**Cómo se juega.** Tres salas unidas por pasillos con trampas. Los enemigos nuevos protegen el hierro.

| Enemigo | Modelo | Vida | Daño | Comportamiento | Suelta |
|---|---|---|---|---|---|
| Orco herrero | `character-orc` oscuro, con `shield-rectangle` | 6 | 2 | Lento; para el primer golpe de cada 3 con el escudo | 2 monedas + hierro (60 %) |
| Bombardero | `character-orc` rojizo | 3 | 3 en área | Se queda lejos y lanza una bomba: círculo de aviso 1,2 s y explosión (como el golpe en área de Urzog) | 1 moneda + hierro (40 %) |
| Trampa de fuego | `trap` | — | 2 | Como la trampa de ahora, pero visible y se activa a ritmo fijo (2 s de 4) | — |

**Jefe: Grimbra la Fundidora** (orca, `character-orc` al 150 %, color cobre). Vida 25.
- Golpe en área como Urzog.
- Con media vida, llama a 2 orcos herreros.
- Frases: al entrar "La forja no se apaga. Yo tampoco." · al caer "Golpea más fuerte que el martillo. Bien."

**Botín:** sobre todo hierro. Primera vez: la herramienta **pico** fabricable y una **bomba** de regalo.

```
######################
#E....#.......#......#
#.....#...h...#..$...#
#.....X.......X......#
#.....#...$...#..b...#
##.####.......####.###
#......X.....X.......#
#..h.................#
#.......#######......#
#..$....#.....#...p..#
#.......#..J..#......#
#..b....#.....#...b..#
#.......###.###......#
#.....F..........F...#
#..h..............h..#
######################
h: orco herrero · b: bombardero · F: trampa de fuego
```

## 3. Mazmorra 3 — la Torre del Vigía

**Lore.** La torre desde la que los orcos vigilaban el valle. El Vigía sigue arriba, contando a los que suben.
Ansel: "Tres plantas. Nadie ha bajado de la tercera."

**Cómo se juega.** Tres plantas pequeñas; cada una es un mapa y se pasa por una escalera (`stairs`), que
funciona como las puertas de ahora. Muchos arqueros: hay que acercarse cubriéndose con columnas.

| Enemigo | Modelo | Vida | Daño | Comportamiento | Suelta |
|---|---|---|---|---|---|
| Arquero de la torre | arquero de ahora, más alcance | 3 | 1 | Dispara desde lejos; huye si te acercas (como los Cazadores del Vado) | 1 moneda + hueso (40 %) |
| Vigía | `character-human` gris, con `weapon-spear` | 4 | 2 | Si te ve, toca la campana: todos los enemigos de la planta van a por ti | 2 monedas + hueso (50 %) |
| Perro de la torre | `character-orc` pequeño (60 %) | 2 | 1 | Muy rápido, va directo a ti | hueso (50 %) |

**Jefe: el Vigía Mayor** (`character-human` al 140 %, con `weapon-spear`). Vida 22.
- Lluvia de flechas: 3 círculos de aviso a la vez cerca de ti.
- Cada 25 % de vida perdida, salta a otra esquina de la sala y llama a 2 perros.
- Frases: "Te vi entrar en el valle." · "Desde aquí arriba se ve todo. Ahora lo ves tú."

**Botín:** sobre todo hueso. Primera vez: **sabueso** y **señuelo** fabricables y la pieza **campana** para el fuerte.

```
Planta 1                Planta 2                Planta 3 (jefe)
##############          ##############          ##############
#E...........#          #S..a....a..S#          #S...........#
#..a.....a...#          #...c....c...#          #..c......c..#
#...c....c...#          #.v..........#          #............#
#......$.....#          #....$...v...#          #.....J......#
#..v.........#          #..r......r..#          #............#
#....c....c..#          #...c....c...#          #..c......c..#
#.p.........S#          #.p..........#          #..p......$..#
##############          ##############          ##############
a: arquero · v: vigía · r: perro · c: columna · S: escalera
```

## 4. Mazmorra 4 — el Pozo de los Clanes

**Lore.** El pozo donde los clanes se mataron por el trono. Sus fantasmas siguen peleando: cada clan
quiere ser el último. Gruk: "Ahí abajo luchó mi padre. Perdió. Lleva cien años perdiendo."

**Cómo se juega.** Una arena redonda. Cuatro oleadas, una por clan; entre oleadas, 10 s para recoger y
beber pociones. Si mueres, pierdes la mitad como siempre; si sales entre oleadas, te llevas lo ganado.

| Oleada | Clan | Enemigos |
|---|---|---|
| 1 | Colmillo Roto | 6 orcos |
| 2 | Cazadores del Vado | 4 arqueros + 2 orcos |
| 3 | Guardia del Rey Muerto | 5 esqueletos + 2 vigías |
| 4 | Clan de la Forja | 3 orcos herreros + 2 bombarderos |

**Jefe: Kargash, Último de los Clanes** (`character-orc` al 170 %, negro y dorado, con `banner` a la espalda). Vida 40.
- Fase 1: golpe en área y carga en línea recta (aviso 1 s).
- Fase 2 (media vida): llama a un guerrero de cada clan anterior, uno cada 10 s.
- Frases: "Cien años esperando a alguien que no fuera un orco." · "Entonces el trono es tuyo. Que te dure."

**Botín:** de todo, sobre todo monedas. Primera vez: la unidad **Kargash defensor** (como el orco jefe) y
las piezas **torre con arquero** y **perro guardián**.

```
########################
#####......E.......#####
###..................###
##....c..........c.....##
#......................#
#..........J...........#
#......................#
##....c..........c.....##
###..................###
#####......p.......#####
########################
c: columna. Los enemigos de cada oleada salen por los bordes.
```

## 5. Números (al balance)

| | Mazmorra 1 (hoy) | Mazmorra 2 | Mazmorra 3 | Mazmorra 4 |
|---|---|---|---|---|
| Vida del jefe | 15 | 25 | 22 | 40 |
| Multiplicador de vida de enemigos | ×1 | ×1,3 | ×1,5 | ×1,8 |
| Máximo por entrada | 60 monedas, 10 hierro, 12 hueso | 60 monedas, 25 hierro, 10 hueso | 70 monedas, 10 hierro, 25 hueso | 120 monedas, 20 hierro, 20 hueso |
| Jefe, 1.ª vez del día | 3 hueso + 2 hierro | 2 hueso + 6 hierro | 6 hueso + 2 hierro | 5 hueso + 5 hierro + 20 monedas |
| Duración esperada | 4-6 min | 5-7 min | 6-8 min | 7-9 min |

## 6. Qué abre cada jefe

| Jefe vencido | Saqueo | Fuerte |
|---|---|---|
| Urzog (M1) | — | Orco jefe defensor (hoy) |
| Grimbra (M2) | Pico y bomba fabricables | Doble fondo (escondite), muro de hierro |
| Vigía Mayor (M3) | Sabueso y señuelo fabricables | Campana |
| Kargash (M4) | — | Kargash defensor, torre con arquero, perro guardián |

## 7. Preguntas para Daniel (respondidas: sí a las seis propuestas)

1. **Modelos.** Con lo que hay (orco, humano y Mini Characters, cambiando color y tamaño) salen todos los
   enemigos de arriba, pero se parecen entre sí. *Propuesta:* importar dos packs gratuitos de Kenney
   (Graveyard Kit y Castle Kit) para decorar la Forja y la Torre. El disco C tiene menos de 2 GB libres:
   habría que liberar espacio antes.
2. **La Torre en tres plantas** (tres mapas con escaleras) o en un mapa grande. *Propuesta:* tres plantas.
3. **El Pozo por oleadas** en vez de salas. *Propuesta:* sí, es la mazmorra "final" y se juega distinto.
4. **Defensores y trampas en el saqueo** (08 §3.3, "versión siguiente"): ¿se activan al sacar la mazmorra 4
   para todos, o cuando cada jugador vence a Kargash? *Propuesta:* para todos a la vez, con un aviso del panel,
   porque cambia el saqueo de todos.
5. **Orden de construcción.** *Propuesta:* una mazmorra por fase (14, 15, 16), cada una con su commit, y
   probándola en el emulador antes de pasar a la siguiente.
6. **Ansel vende el mapa** de la mazmorra (07-lore §5.5: ver trampas y salas antes de entrar). *Propuesta:*
   dejarlo para después de las tres mazmorras.
