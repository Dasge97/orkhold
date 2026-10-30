**English** · [Español](README.es.md)

<div align="center">

<img src="docs/img/icono.png" alt="Orkhold icon: a full-body green orc on a dark green background" width="120">

# Orkhold

**Low-poly online mobile game: explore a shared village, clear dungeons, build your fort and raid other players' forts.**

![Version](https://img.shields.io/badge/version-0.9-blue)
![Platform](https://img.shields.io/badge/platform-Android-3DDC84?logo=android&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-6000.5-000000?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![Node](https://img.shields.io/badge/Node-22-5FA04E?logo=nodedotjs&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/license-all_rights_reserved-lightgrey)
![Last change](https://img.shields.io/badge/last_change-2026--10--30-informational)

![The village on the Android emulator: the hero walks along a dirt path between two houses with banners; an orc and coins in the background; joystick on the left and run, attack and pick-up buttons on the right](docs/img/portada.png)

[![Install](https://img.shields.io/badge/Install-APK-3DDC84?style=for-the-badge&logo=android&logoColor=white)](#installation)
[![How it works](https://img.shields.io/badge/How_it_works-docs-5C6BC0?style=for-the-badge)](docs/02-arquitectura.md)
[![Game design](https://img.shields.io/badge/Game_design-docs-26A69A?style=for-the-badge)](docs/01-diseno.md)

</div>

## What it is for

Orkhold is a top-down 3D mobile game made with Unity 6 and Kenney's CC0 low-poly packs. Players
share an online **village** where they see each other move, chop trees, mine rocks and fight orcs.
From the village they enter the **dungeon**, a separate map with enemies, traps, a boss and loot.
Each player owns a **fort** built from what they gather: walls, doors, traps and defenders. PvP means
**raiding** another player's fort and reaching its chest within 90 seconds.

The code, comments and docs are in Spanish.

## What it does

| Part | What there is |
|---|---|
| Online village | Several players in the same village with Netcode for GameObjects; the server moves everyone, runs the orcs and decides the loot |
| Tutorial | Five signs next to the campfire explain moving, attacking, picking up, the dungeon and the fort |
| Dungeon | Catacombs with orcs, archers, skeletons, traps and a boss that grants a unit; dying loses half the loot |
| Fort | Tile editor: 7 pieces with cost, limits and defender upgrades |
| Raid | 90 s to reach a rival's chest; Elo-like ranking and history |
| Accounts | Node 22 + Fastify 5 + PostgreSQL 16 API: device account, inventory, forts and raids |
| Offline | Village and dungeon work locally; fort and raid need the API |

<table>
<tr>
<td><img src="docs/img/tutorial.png" alt="Tutorial sign in the village: a dark box at the top explains that the character moves with the left joystick and runs while the run button is held"></td>
<td><img src="docs/img/mazmorra.png" alt="Dungeon room with stone floor: the hero fights two green orcs; at the top, health reads 3 of 6"></td>
</tr>
<tr>
<td><img src="docs/img/construir.png" alt="Fort editor: tile grid seen from above with two wooden wall sections and the chest; at the bottom, the piece bar with costs"></td>
<td><img src="docs/img/asalto.png" alt="Raiding a fort: the hero inside the rival's fence, with the timer at 1:26 and the distance-to-chest bar at the top"></td>
</tr>
<tr>
<td><img src="docs/img/arranque.png" alt="Create-character screen: the name ORKHOLD at the top, model selector, name field, Start button and the chosen character on the right"></td>
<td><img src="docs/img/carga.png" alt="Black loading screen with the name ORKHOLD in gold and a tip below"></td>
</tr>
</table>

## Installation

**Play:** build the APK (see Usage; it is not in git) and install it on an Android phone (ARM64, Android 8 or later):

```bash
adb install -r Builds/orkhold.apk
```

**Develop:** Unity Hub with **Unity 6000.5.9f1** and the Android module. Docker for the API.

```bash
git clone https://github.com/Dasge97/orkhold.git
# Open the folder with Unity Hub. Scenes build themselves from Assets/Mapas/*.txt
# (or menu Prototipo > Montar escenas).
cd orkhold/api && docker compose up -d --build   # API at http://127.0.0.1:3100
```

The API needs `api/.env` with `POSTGRES_PASSWORD`, `API_SECRETO` and `CLAVE_SERVIDOR`. That file is
not in git.

## Usage

| Phone | Keyboard (editor) | Action |
|---|---|---|
| Joystick | WASD / arrows | Move |
| Sword button | Space | Attack: trees give wood, rocks stone, orcs coins |
| Hand button | E | Pick up, open doors, use the board or the raid chest |
| Arrow button (held) | Left Shift | Run |

Editor tools (menu **Prototipo**) and command line:

| What | How |
|---|---|
| Build scenes | `Prototipo > Montar escenas` |
| Review photos | `Prototipo > Revisar` (saves photos in `Revision/`) |
| APK | `Unity -batchmode -buildTarget Android -executeMethod CompilarAndroid.EnBatch -urlApi <url> -mundoApk <host:port>` |
| Tests | `Unity -batchmode -runTests -testPlatform PlayMode` (44 tests) and `docker compose run --rm api npm test` (31 tests) |
| Network test | `powershell -File pruebas/red-local.ps1`: one server and two bots that must see each other move |

## Configuration

| Where | What |
|---|---|
| `Assets/Resources/Configuracion.asset` | API URL and world server. Empty in git (local mode); set when building the APK |
| Executable arguments | `-servidor -puerto 7777` (world server), `-mundo host:port`, `-api URL`, `-bot` |
| World server variables | `ORKHOLD_API`, `ORKHOLD_CLAVE_SERVIDOR`, `ORKHOLD_ESTADO` |
| `deployment/` | Production compose (PostgreSQL, API, world server), Traefik and codehive scripts |

Production secrets live in the Vaultwarden vault, never in the repository.

## How it works

```
Phone (Unity) ──HTTPS──> API (Fastify) ──> PostgreSQL
     │                        ▲
     └──UDP 7777──> World server (headless Unity) ──server key──┘
```

- [`docs/01-diseno.md`](docs/01-diseno.md): the game and its numbers.
- [`docs/02-arquitectura.md`](docs/02-arquitectura.md): client, network, API, tables, text maps and deployment.
- [`docs/03-tareas.md`](docs/03-tareas.md): the nine phases and their status.
- [`docs/05-diario.md`](docs/05-diario.md) and [`docs/06-decisiones.md`](docs/06-decisiones.md): log and decisions.

Art: [Kenney](https://kenney.nl) packs Mini Dungeon, Mini Characters, Nature Kit and Mobile Controls, all CC0.

## License

All rights reserved. Kenney assets are CC0.
