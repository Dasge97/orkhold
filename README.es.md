[English](README.md) · **Español**

<div align="center">

<img src="docs/img/icono.png" alt="Icono de Orkhold: un orco verde de cuerpo entero sobre fondo verde oscuro" width="120">

# Orkhold

**Juego de móvil online low-poly: explora un pueblo compartido, supera mazmorras, construye tu fuerte y asalta los fuertes de otros jugadores.**

![Versión](https://img.shields.io/badge/versi%C3%B3n-0.9-blue)
![Plataforma](https://img.shields.io/badge/plataforma-Android-3DDC84?logo=android&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-6000.5-000000?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![Node](https://img.shields.io/badge/Node-22-5FA04E?logo=nodedotjs&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![Licencia](https://img.shields.io/badge/licencia-todos_los_derechos_reservados-lightgrey)
![Último cambio](https://img.shields.io/badge/%C3%BAltimo_cambio-2026--10--30-informational)

![El pueblo en el emulador de Android: el héroe anda por un camino de tierra entre dos casas con estandartes; un orco y monedas al fondo; joystick a la izquierda y botones de correr, atacar y recoger a la derecha](docs/img/portada.png)

[![Instalar](https://img.shields.io/badge/Instalar-APK-3DDC84?style=for-the-badge&logo=android&logoColor=white)](#instalación)
[![Cómo funciona](https://img.shields.io/badge/C%C3%B3mo_funciona-docs-5C6BC0?style=for-the-badge)](docs/02-arquitectura.md)
[![Diseño](https://img.shields.io/badge/Dise%C3%B1o_del_juego-docs-26A69A?style=for-the-badge)](docs/01-diseno.md)

</div>

## Para qué sirve

Orkhold es un juego de móvil en 3D visto desde arriba, hecho con Unity 6 y los packs low-poly CC0
de Kenney. Los jugadores comparten un **pueblo** en línea, donde se ven moverse, talan árboles,
pican rocas y pelean con orcos. Desde el pueblo se entra a la **mazmorra**, un mapa aparte con
enemigos, trampas, un jefe y botín. Cada jugador tiene un **fuerte** propio que construye con lo que
recoge: muros, puertas, trampas y defensores. El PvP es **asaltar** el fuerte de otro jugador y
llegar a su cofre antes de 90 segundos.

## Qué hace

| Parte | Qué hay |
|---|---|
| Pueblo online | Varios jugadores en el mismo pueblo con Netcode for GameObjects; el servidor mueve a todos, lleva los orcos y decide el botín |
| Tutorial | Cinco carteles junto a la fogata explican moverse, atacar, recoger, la mazmorra y el fuerte |
| Mazmorra | Catacumbas con orcos, arqueros, esqueletos, trampas y un jefe que da una unidad; si mueres, pierdes la mitad del botín |
| Fuerte | Editor por casillas: 7 piezas con coste, límites y mejoras de defensores |
| Asalto | 90 s para llegar al cofre de un rival; ranking con puntuación tipo Elo e historial |
| Cuentas | API Node 22 + Fastify 5 + PostgreSQL 16: cuenta por dispositivo, inventario, fuertes y asaltos |
| Sin conexión | El pueblo y la mazmorra se juegan en local; el fuerte y el asalto necesitan la API |

<table>
<tr>
<td><img src="docs/img/tutorial.png" alt="Cartel del tutorial en el pueblo: un recuadro oscuro arriba explica que el personaje se mueve con el joystick de la izquierda y que se corre manteniendo pulsado el botón de correr"></td>
<td><img src="docs/img/mazmorra.png" alt="Sala de la mazmorra con suelo de piedra: el héroe pelea con dos orcos verdes; arriba, la vida marca 3 de 6"></td>
</tr>
<tr>
<td><img src="docs/img/construir.png" alt="Editor del fuerte: rejilla de casillas vista desde arriba con dos tramos de muro de madera y el cofre; abajo, la barra de piezas con su coste"></td>
<td><img src="docs/img/asalto.png" alt="Asalto a un fuerte: el héroe dentro de la valla del rival, con el reloj a 1:26 y la barra de distancia al cofre arriba"></td>
</tr>
<tr>
<td><img src="docs/img/arranque.png" alt="Pantalla de crear personaje: el nombre ORKHOLD arriba, selector de modelo, campo para el nombre, botón Empezar y el personaje elegido a la derecha"></td>
<td><img src="docs/img/carga.png" alt="Pantalla de carga negra con el nombre ORKHOLD en dorado y un consejo debajo"></td>
</tr>
</table>

## Instalación

**Jugar:** compila el APK (ver Uso; no va en git) e instálalo en un Android (ARM64, Android 8 o superior):

```bash
adb install -r Builds/orkhold.apk
```

**Desarrollar:** Unity Hub con **Unity 6000.5.9f1** y el módulo de Android. Para la API, Docker.

```bash
git clone https://github.com/Dasge97/orkhold.git
# Abre la carpeta con Unity Hub. Las escenas se montan solas desde Assets/Mapas/*.txt
# (o menú Prototipo > Montar escenas).
cd orkhold/api && docker compose up -d --build   # API en http://127.0.0.1:3100
```

La API necesita `api/.env` con `POSTGRES_PASSWORD`, `API_SECRETO` y `CLAVE_SERVIDOR`. Ese fichero no
va en git.

## Uso

| Móvil | Teclado (editor) | Acción |
|---|---|---|
| Joystick | WASD / flechas | Moverse |
| Botón espada | Espacio | Atacar: árboles dan madera, rocas piedra, orcos monedas |
| Botón mano | E | Recoger, abrir puertas, usar el tablón o el cofre de asalto |
| Botón flecha (mantenido) | Shift izquierdo | Correr |

Herramientas del editor (menú **Prototipo**) y en línea de órdenes:

| Qué | Cómo |
|---|---|
| Montar escenas | `Prototipo > Montar escenas` |
| Fotos de revisión | `Prototipo > Revisar` (deja fotos en `Revision/`) |
| APK | `Unity -batchmode -buildTarget Android -executeMethod CompilarAndroid.EnBatch -urlApi <url> -mundoApk <host:puerto>` |
| Tests | `Unity -batchmode -runTests -testPlatform PlayMode` (44 tests) y `docker compose run --rm api npm test` (31 tests) |
| Prueba de red | `powershell -File pruebas/red-local.ps1`: un servidor y dos bots que se tienen que ver moverse |

## Configuración

| Dónde | Qué |
|---|---|
| `Assets/Resources/Configuracion.asset` | URL de la API y servidor de mundo. En git van vacíos (modo local); se ponen al compilar el APK |
| Argumentos del ejecutable | `-servidor -puerto 7777` (servidor de mundo), `-mundo host:puerto`, `-api URL`, `-bot` |
| Variables del servidor de mundo | `ORKHOLD_API`, `ORKHOLD_CLAVE_SERVIDOR`, `ORKHOLD_ESTADO` |
| `deployment/` | Compose de producción (PostgreSQL, API, servidor de mundo), Traefik y scripts de codehive |

Los secretos de producción viven en la bóveda Vaultwarden, nunca en el repositorio.

## Cómo funciona

```
Móvil (Unity) ──HTTPS──> API (Fastify) ──> PostgreSQL
     │                        ▲
     └──UDP 7777──> Servidor de mundo (Unity sin ventana) ──clave de servidor──┘
```

- [`docs/01-diseno.md`](docs/01-diseno.md): el juego y sus números.
- [`docs/02-arquitectura.md`](docs/02-arquitectura.md): cliente, red, API, tablas, mapas en texto y despliegue.
- [`docs/03-tareas.md`](docs/03-tareas.md): las nueve fases y su estado.
- [`docs/05-diario.md`](docs/05-diario.md) y [`docs/06-decisiones.md`](docs/06-decisiones.md): diario y decisiones tomadas.

Arte: packs de [Kenney](https://kenney.nl) Mini Dungeon, Mini Characters, Nature Kit y Mobile Controls, todos CC0.

## Licencia

Todos los derechos reservados. Los recursos de Kenney son CC0.
