# Orkhold — prototipo PvE (Unity 6000.5.9f1)

Proyecto montado a mano, sin abrir Unity. La primera vez que se abre:

1. Unity importa los modelos de `Assets/Kenney/` (Mini Dungeon, Nature Kit, Mobile Controls; todos CC0).
2. `Assets/Editor/ImportadorKenney.cs` deja los clips `idle`, `walk` y `sprint` en bucle.
3. `Assets/Editor/MontarEscenaPvE.cs` crea `Assets/Scenes/PvE.unity` sola: suelo, árboles, mazmorra,
   jugador, 6 orcos, monedas, pociones, cámara, joystick, 3 botones y HUD.
   Si no se crea sola, menú **Prototipo > Montar escena PvE**.

## Controles

| Móvil | Teclado (editor) | Qué hace |
|---|---|---|
| Joystick | WASD / flechas | Moverse |
| Botón espada | Espacio | Atacar (1 de daño; los orcos tienen 3 de vida) |
| Botón mano | E | Recoger lo más cercano; si no hay nada y falta vida, beber poción |
| Botón flecha (mantenido) | Shift izq. | Correr |

## Scripts

- `EntradaJuego` — entrada unificada (pantalla o teclado).
- `JoystickVirtual`, `BotonAccion` — controles flotantes.
- `ControlJugador` — moverse, atacar, recoger, morir y reiniciar.
- `Enemigo` — esperar, perseguir a 5 m, pegar a 1 m, soltar moneda al morir.
- `Vida`, `Inventario`, `Recogible`, `CamaraSigue`, `HUD`.

Las animaciones se eligen por nombre de estado con `CrossFade`; el AnimatorController
se genera en `Assets/Animacion/` con un estado por clip del FBX, sin transiciones.

## Compilar para Android

File > Build Profiles > Android > Switch Platform > Build. Orientación apaisada ya fijada.
