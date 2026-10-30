# 08 · Formato de juego y PvP: el saqueo

Fecha: 2026-10-30. Sustituye el asalto de la primera versión (`01-diseno.md` §5.2, "llegar al cofre
en 90 s") por el modelo que Daniel describió el 30-10-2026 y cerró ese mismo día con las respuestas
del §8. Lo marcado como *propuesta* son números o detalles que él puede cambiar; el modelo en sí está decidido.

## 1. Formato de juego

### 1.1 Qué es una sesión
Una sesión son **5 minutos** y tiene que dar siempre algo:
- **Recolectar** (2-3 min): una vuelta por Vado Gris, talar, picar, matar dos orcos. Sales con material.
- **Mazmorra** (4-6 min): bajar, llegar al jefe o morir. Sales con hierro y hueso.
- **Saquear** (3 min): un saqueo y su resultado. Sales con el botín del otro, o con nada.
- **Esconder** (2 min): reorganizar tus escondites después de que te hayan saqueado.

### 1.2 Ritmo diario (*propuesta*, números de balance)
| Qué | Valor inicial |
|---|---|
| Saqueos por día | 10. Se recargan a las 6:00 hora del servidor. |
| Enemigos del pueblo | Reaparecen cada 60 s (como hoy). |
| Árboles y rocas | Reaparecen cada 5 min. |
| Jefe de mazmorra | Hueso y hierro una vez al día; después, monedas. |
| Escudo | Tras ser saqueado con éxito, **2 horas** sin poder ser elegido como rival. Si el atacante no encontró nada, no hay escudo. |

### 1.3 Temporadas (*propuesta*)
4 semanas. Al acabar, la puntuación se comprime hacia 1000 y se reparte un estandarte (`banner` de
un color) por tramo: Bronce, Plata, Oro, Hierro Orco. Solo decorativo; el atacante lo ve al entrar.

### 1.4 Ejes de progreso
1. **El fuerte**: más piezas, más y mejores escondites, defensas.
2. **Las mazmorras**: cada una desbloquea una herramienta de saqueo y una pieza de defensa (`09-plan-version-2.md`).
3. **El personaje** (más adelante): equipo con 2-3 huecos, sin niveles numéricos.

### 1.5 Dinero real
Ninguno. Si algún día entra: solo estandartes y colores. Nunca materiales, acciones ni saqueos extra.

### 1.6 Balance fuera del código
Todos los números de este documento viven en la **tabla de balance** de la base de datos, editable
desde el panel de administración (`10-panel-admin.md`). El cliente y el servidor de mundo la descargan
al arrancar. Ningún número de balance se escribe fijo en el código.

## 2. El saqueo en una frase

**El defensor esconde todo lo que tiene en su fuerte. El atacante entra, se mueve, rompe muros a golpes
y mira; después elige unos pocos escondites donde buscar, y el servidor le dice qué ha encontrado.**
Las herramientas sirven para buscar mejor o romper más. Quien defiende bien, cobra.

## 3. Defender: esconder

### 3.1 La obligación
**Todo lo que posees está escondido en tu fuerte.** No hay inventario aparte ni tope numérico: el tope
es la capacidad de tus escondites. Si están llenos, no puedes recoger más; el juego avisa
"Escondites llenos: construye o gasta". Gastar en construir también es protegerse.

Los recursos recogidos en el pueblo o la mazmorra van a parar a tus escondites al volver (al tocar la
Brasa o al entrar en tu parcela), repartidos en el primer escondite con hueco. El jugador puede
recolocarlos a mano en el modo construir.

### 3.2 Tipos de escondite
| Escondite | Modelo | Cuesta | Capacidad | Se ve | Al acertar se lleva |
|---|---|---|---|---|---|
| **Cofre central** | `chest` grande, fijo en el centro | — | 50 | Sí, siempre, y todo el mundo sabe dónde está | 75 % |
| **Cofre** | `chest` | 4 madera + 1 hierro | 30 | Sí | 50 % |
| **Barril** | `barrel` | 2 madera | 15 | Sí | 50 % |
| **Enterrado** | casilla de suelo sin nada encima | 1 piedra | 10 | **No** (la casilla parece normal) | 50 % |
| **Doble fondo** (mazmorra 2) | `wall` con hueco | 3 piedra + 1 hierro | 10 | No (parece un muro) | 50 % |

Límites iniciales (*propuesta*): 4 cofres, 6 barriles, 8 enterrados. Suben con las mazmorras.
La "unidad" es la misma para todos los recursos (1 moneda = 1 madera = 1 unidad). Un escondite se
llena a mano en el modo construir: tocas el escondite y eliges cuánto de qué. Un cofre o barril
**vacío** es un señuelo legítimo: el atacante no sabe si hay algo hasta que lo abre o lo mira (§4.2).

### 3.3 Protección
Primera versión del saqueo:
| Protección | Qué hace |
|---|---|
| Muros y puertas | El atacante los rompe **a golpes durante la exploración**, como hoy (madera 5 golpes, piedra 10, puerta 6). Cuestan tiempo, no acciones. Un escondite rodeado de piedra es un escondite al que quizá no llegue antes de que se acabe el tiempo. |
| Esconder bien | Mezclar escondites llenos y vacíos, enterrar, poner el cofre central casi vacío. |

Versión siguiente (decidido: "más adelante se podrán poner trampas y defensas"):
| Protección | Qué hace |
|---|---|
| Trampa de pinchos | Hace daño al pisarla explorando, como hoy. Además, si hay una trampa sin pisar en la casilla de un escondite marcado, la primera acción sobre él se pierde. |
| Defensores (orco, esqueleto, perro, torre) | Pelean con el atacante en la exploración. Si el atacante muere, el saqueo termina sin botín. Un defensor vivo a 3 casillas de un escondite marcado hace fallar la búsqueda, salvo señuelo. |
| Cofre falso, campana, muro de hierro | Ver §6. |

Hasta entonces, las trampas y defensores que ya existen en el fuerte **no actúan** en el saqueo (se
quedan en el fuerte, pero el servidor los ignora). Así la primera versión del saqueo se prueba solo
con esconder y romper.

### 3.4 Lo que el defensor pierde y lo que gana
- Si el atacante acierta un escondite, se lleva el porcentaje de la tabla (§3.2), redondeado abajo.
  Lo que no encuentran no se pierde. Nunca se pierde todo.
- Los muros rotos vuelven a estar al terminar el saqueo (la Brasa los repone, `07-lore.md`). Solo se mueven los recursos.
- **Recompensa por defender bien** (decidido el 30-10-2026, números *propuesta*): el atacante paga una
  **tasa de 5 monedas** al entrar. Si sale sin nada, la tasa va entera al defensor y el defensor gana
  Elo. Si el atacante encuentra algo, la tasa se pierde (la "cobra" la Brasa) y el defensor no recibe nada.

## 4. Atacar: saquear

### 4.1 Antes de entrar
- Pantalla **"Preparar saqueo"**: nombre, estandarte y tramo del rival y el **número** de piezas y
  escondites visibles de su fuerte (no dónde). Eliges hasta **2 herramientas** de las que tengas (§5).
  Se cobra la tasa (§3.4).
- Rival: puntuación parecida, sin escudo, no saqueado por ti en 24 h. Si no hay nadie, un fuerte de
  prueba sin botín real ni tasa.

### 4.2 Fase 1: explorar (120 s, *propuesta*; sin gastar acciones)
Entras con tu personaje y los controles de siempre:
- **Moverte** por el fuerte y **romper muros y puertas a golpes**, como hoy.
- **Mirar** (botón interactuar sobre un cofre, barril o casilla): texto corto. Cofre o barril: "Pesa."
  (tiene algo) o "Suena hueco." (vacío). Casilla de suelo: "Tierra normal." siempre, salvo con farol (§5).
  Mirar cuesta **1 s** parado, no acciones. Mirar es lo que convierte el saqueo en algo que se juega y no en azar.
- Primera versión: **no hay daño ni muerte** en la exploración. Versión siguiente: trampas y defensores actúan (§3.3).
- Al acabar el tiempo o al pulsar **"Saquear"**, pasas a la fase 2.

### 4.3 Fase 2: marcar escondites
El juego se para y la cámara sube. Tienes **3 acciones** (número de balance; ver §4.6). Tocas cofres,
barriles o casillas de suelo para marcarlos. Cada marca cuesta **1 acción**. Marcar una casilla de suelo
es apostar a un enterrado. Solo se puede marcar lo que está **al alcance**: un escondite dentro de un
recinto cuyo muro no rompiste no se puede marcar (el servidor comprueba que hay camino libre desde la
entrada con los muros rotos en la exploración).

### 4.4 Fase 3: resolución (el servidor decide)
Envías la lista de marcas más tus entradas de la exploración (movimientos y golpes con su tiempo). El servidor:
1. Rehace la exploración con el mapa real: qué muros se rompieron de verdad y si cada marca tiene camino.
   Descarta entradas imposibles (velocidad, golpes sin muro delante).
2. Para cada marca, en orden: si hay escondite y tiene algo, calcula el botín (§3.2) y lo mueve de un
   fuerte al otro. Si está vacío o no hay nada, "nada".
3. Aplica tasa, Elo y escudo (§3.4, §4.5) y guarda el **informe** para el defensor.
En el móvil se enseña como una secuencia corta: tu personaje corre a cada marca, abre o cava, y sale
el número. Diez segundos. Después, la pantalla de resultado.

### 4.5 Puntuación (*propuesta*)
| Resultado | Atacante | Defensor |
|---|---|---|
| No encontró nada | Elo -10, pierde la tasa | Elo +10, cobra la tasa |
| Encontró algo en 1 marca | Elo +5, botín | Elo -5 |
| Encontró algo en 2 o más marcas | Elo +15, botín | Elo -10 |
| Entre lo encontrado está el cofre central con 20 o más unidades | +5 más | -5 más (castigo por no esconder) |

El defensor ve en su informe, sobre su rejilla, qué rompió, qué miró y qué marcó el atacante, y qué
encontró. Con eso sabe qué escondite está quemado y dónde cambiar.

### 4.6 Cuántas acciones (decidido el 30-10-2026)
El número depende de cómo esté el juego. Regla de balance: **la mayoría de los saqueos tienen que dar
algo**. Se empieza con 3. Si en el panel de administración se ve que menos del 60 % de los saqueos
encuentran algo, se sube a 4; si más del 85 %, se baja a 2. Es un número de la tabla de balance, no del código.

## 5. Herramientas de saqueo

Las fabrica Ferra. Se gastan al usarlas. Máximo 2 por saqueo. (Costes *propuesta*.)
| Herramienta | Cuesta | Qué hace | Fase | Desbloqueo |
|---|---|---|---|---|
| **Farol** | 2 hierro + 1 hueso | Mientras lo llevas, mirar una casilla de suelo dice "Tierra removida." si hay algo enterrado en ella o en las 8 de alrededor | Explorar | Inicio |
| **Cuarta acción** | 10 monedas | Una acción más en la fase 2 | Marcar | Inicio |
| **Pico** | 3 hierro + 2 madera | Durante 15 s, cada golpe hace 3 de daño a muros y puertas | Explorar | Mazmorra 2 |
| **Bomba** | 4 hierro + 2 piedra | Tras 2 s destruye todos los muros y puertas de un 3x3 | Explorar | Mazmorra 2 |
| **Sabueso** | 3 hueso + 2 monedas | Al entrar, el perro corre hacia el escondite **con más unidades** y ladra a 3 casillas de él. No dice cuál exactamente | Explorar | Mazmorra 3 |
| **Señuelo** | 2 hueso + 1 madera | Un defensor elegido no pelea ni protege | Explorar y marcar | Mazmorra 3 (cuando haya defensores en el saqueo) |

## 6. Piezas de defensa nuevas (versión siguiente del saqueo)
| Pieza | Cuesta | Qué hace | La contrarresta |
|---|---|---|---|
| **Perro guardián** | 8 monedas | Defensor pequeño y rápido: ve a 6 casillas, avisa a los demás | Señuelo, o matarlo explorando |
| **Campana** | 3 hierro | Si el atacante pasa a 3 casillas, quita **20 s** de exploración | Rodearla |
| **Torre con arquero** | 6 piedra + 8 monedas | Dispara a 6 casillas explorando; protege como defensor a 4 casillas al marcar | Bomba, o quedarse fuera de su vista |
| **Muro de hierro** | 2 hierro + 2 piedra | Vida 10 y el pico no le hace daño extra | Bomba |
| **Doble fondo** | 3 piedra + 1 hierro | Escondite dentro de un muro (§3.2) | Mirar el muro con farol dice "Suena hueco." |

## 7. Por qué este modelo arregla lo de antes
| Problema del asalto de 90 s | Ahora |
|---|---|
| El resultado lo decidía el móvil del atacante | Lo decide el servidor: rehace la exploración y resuelve las marcas |
| Solo una forma de atacar (golpear) | Explorar, mirar, romper, elegir, herramientas |
| Solo una forma de defender (anillo de piedra) | Esconder bien, mezclar vacíos y llenos, o encerrar en piedra para que no le dé tiempo |
| El defensor no participaba | Decide dónde esconde, cobra si defiende bien y cambia tras cada informe |
| Todo o nada | Cada marca es un resultado |
| El defensor no perdía nada | Pierde parte de lo que le encuentran; nunca todo |

## 8. Decisiones del 30-10-2026 (respuestas de Daniel)
| Pregunta | Respuesta |
|---|---|
| Acciones por saqueo | Las que hagan falta para que la mayoría de saqueos dé algo. Empezar con 3 y ajustar desde el panel (§4.6). |
| Pelear y morir explorando | Sí, pero no en la primera versión del saqueo. Trampas y defensores llegan después (§3.3). |
| Lo que está a la vista | No se pierde todo. Cofre central: 75 %. |
| Muros | Se rompen a golpes en la exploración. Las acciones son solo para escondites. |
| Tope de recursos | No hay tope numérico. Todo lo que tienes va escondido; el tope es la capacidad de los escondites. |
| Defender bien | Tiene recompensa: la tasa del atacante y Elo (§3.4). |
