# 04 · Pruebas de asalto

Tarea 5.5: jugar asaltos contra los tres fuertes de prueba y ajustar vida de muros, daño de trampas
y tiempo. Objetivo: el fuerte difícil se gana en ~60 s jugando bien y se pierde jugando mal.

**Pendiente:** la prueba con Daniel (10 asaltos a mano). Mientras tanto, la hace un jugador
automático (`Assets/Tests/PlayMode/BotAsalto.cs`) en el test
`PruebaAsalto.JugadorAutomaticoContraLosTresFuertes`, que deja sus partidas en `Revision/asaltos.txt`.

## Cómo juega el jugador automático
- **Bueno**: busca el camino más barato al cofre por la rejilla (romper un muro cuesta según su vida;
  evita trampas y defensores), pelea con los defensores que se le acercan y, si le quedan 3 de vida o
  menos y quedan más de 35 s, huye corriendo a la entrada hasta recuperar vida.
- **Malo**: va por el camino más corto, pisa trampas y no se defiende.

## Fuertes de prueba (`Assets/Mapas/fuertes-prueba`, generados con `generar.py`)
| Fuerte | Muros | Trampas | Defensores |
|---|---|---|---|
| Fácil | 8 de madera en línea (se puede rodear) | 1 | 1 orco junto al cofre |
| Medio | anillo de 16 de piedra alrededor del cofre | 3 delante del lado oeste | 2 orcos dentro del anillo |
| Difícil | 40: línea de madera, anillo de piedra 7x7 y anillo de piedra 3x3 | 6 en el pasillo oeste entre anillos | 2 orcos detrás de la madera y 2 esqueletos entre anillos, todos con la mejora máxima (+3) |

## Ajustes
| Fecha | Cambio | Por qué |
|---|---|---|
| 30-10-2026 | Cada golpe rompe como mucho una pieza del fuerte (la más cercana). | Un golpe rompía hasta tres muros a la vez: el fácil y el medio se ganaban en 5-8 s jugando mal. |
| 30-10-2026 | Vida de muros: madera 3 → 5, piedra 6 → 10, puerta 4 → 6. | Con 3/6 golpes, romper muros es tan rápido (0,6 s por golpe) que ni con 40 muros un asalto pasa de 20 s. |
| 30-10-2026 | Daño de trampas (1) y tiempo (90 s) sin cambios. | Con los muros nuevos, el difícil ya ronda los 60 s. |
| 30-10-2026 | En el asalto los defensores ven a 8 m y persiguen hasta 12 m de su sitio (antes 5 y 7). | Con 5 m a veces el atacante se colaba sin pelear: el difícil salía en 20 s o en 65 s según la partida. |
| 30-10-2026 | Defensores del difícil con la mejora máxima (+3). | Con los defensores vigilando todo el fuerte, el difícil salía siempre en 28 s. |
| 30-10-2026 | Radio para tocar el cofre: 1,25 m (antes 0,95). | En el emulador, pasando pegado al cofre no contaba. |

## Resultados (30-10-2026, jugador automático)
| Fuerte | Bueno | Malo |
|---|---|---|
| Fácil (3 partidas) | gana en 9 s | muere a los 14 s (el orco le persigue) |
| Medio (3 partidas) | gana en 12-14 s | muere a los 12 s |
| Difícil (5 partidas) | gana en 58-59 s con 1 retirada a recuperar vida (4 de 5); una vez muere a los 17 s | muere a los 9 s |

El difícil cumple el criterio (el test pide una media de 35-85 s entre las victorias, "~60 s").
El medio queda fácil jugando bien; se revisará con las pruebas de Daniel.
