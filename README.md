**English** · [Español](README.es.md)

<div align="center">

# Orkhold

**A low-poly online mobile game: explore a shared village, clear dungeons, build your fort, and raid other players' forts.**

![Version](https://img.shields.io/badge/version-0.1_prototype-blue)
![Platform](https://img.shields.io/badge/platform-Android-3DDC84?logo=android&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-6000.5-000000?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/license-private-lightgrey)
![Last change](https://img.shields.io/badge/last_change-2026--10--29-informational)

![Village view on the Android emulator: the hero walks along a dirt path between trees, coins and an orc; virtual joystick on the left, three action buttons on the right, HP and inventory counters top-left](docs/img/portada.png)

[![How it works](https://img.shields.io/badge/How_it_works-docs-5C6BC0?style=for-the-badge)](docs/02-arquitectura.md)
[![Design](https://img.shields.io/badge/Game_design-docs-26A69A?style=for-the-badge)](docs/01-diseno.md)
[![Task list](https://img.shields.io/badge/Task_list-docs-FF7043?style=for-the-badge)](docs/03-tareas.md)

</div>

## What it is

Orkhold is a top-down 3D mobile game built with Unity 6 and Kenney's CC0 low-poly packs.
Players share one **village** map that grows over time. From the village they enter **dungeons**
(each one a separate map and a level, with its own boss and loot), and they own a **fort** on the
edge of the map, built with the materials they collect. The PvP is **raiding** another player's
fort: break walls, dodge traps, beat the defenders and reach the treasure chest in time.

The current state is a playable PvE prototype: village floor, trees, a dungeon room with six
orcs, coins, potions, on-screen controls and HUD, running on an Android emulator.

## What it does today

| Feature | State |
|---|---|
| Virtual joystick + 3 action buttons (attack, interact, run) | Done |
| Hero with walk / sprint / attack / pick-up / die animations | Done |
| Orcs that patrol, chase, hit and drop coins | Done |
| Coins and potions, HP and inventory HUD | Done |
| Scene assembled automatically from Kenney models | Done |
| Automatic review (screenshots + simulated play) and APK build | Done |
| Maps written as text files, dungeons as separate scenes | Planned (phase 1) |
| Fort building and raids | Planned (phases 4-5) |
| Accounts, API, online village | Planned (phases 6-7) |

![The dungeon room seen from above: stone walls, a gate, two barrels, banners, a chest and three orcs](docs/img/mazmorra.png)

## Install

Requirements: Unity Hub with **Unity 6000.5.9f1** and the Android module; for the emulator,
Android Studio with an x86_64 image (ARM translation enabled).

```bash
git clone https://github.com/Dasge97/orkhold.git
# Open the folder with Unity Hub. On first open, Unity imports the models and
# Assets/Editor/MontarEscenaPvE.cs builds Assets/Scenes/PvE.unity on its own.
```

## Use

| Mobile | Keyboard (editor) | Action |
|---|---|---|
| Joystick | WASD / arrows | Move |
| Sword button | Space | Attack (1 damage; orcs have 3 HP) |
| Hand button | E | Pick up the nearest item, or drink a potion if hurt |
| Arrow button (hold) | Left Shift | Run |

Editor menu **Prototipo**:
- **Montar escena PvE** — rebuilds the scene from scratch.
- **Revisar (fotos + informe)** — takes screenshots, enters Play, walks and attacks, writes `Revision/informe.txt`.
- **Compilar APK** — builds `Builds/orkhold.apk` (IL2CPP, ARM64, OpenGL ES 3).

Run on the emulator:

```bash
emulator -avd Medium_Phone_API_36.0 -gpu host
adb install -r Builds/orkhold.apk
adb shell monkey -p com.orkhold.game 1
```

![Raid-style fight: the hero stands at the dungeon wall while two orcs attack him; HP reads 4/6](docs/img/combate.png)

## Configuration

Everything is in code and in `docs/`. There is no configuration file yet. Server, API and
credentials arrive in phases 6-8 (see the task list); secrets will live in the Vaultwarden vault,
never in the repository.

## How it works

- `docs/01-diseno.md` — game design: village, dungeons, fort pieces, raid rules, screens, numbers.
- `docs/02-arquitectura.md` — client, world server (Netcode for GameObjects), API (Node + Fastify), PostgreSQL, text map format, routes and tables.
- `docs/03-tareas.md` — ordered task list, nine phases, each task with its check.
- `docs/05-diario.md` — implementation diary.
- `CLAUDE.md` — working rules for the model that implements the tasks.

Art: [Kenney](https://kenney.nl) packs Mini Dungeon, Mini Characters, Nature Kit and Mobile Controls, all CC0.

## License

Private project. All rights reserved. Kenney assets are CC0.
