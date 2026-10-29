# 05 · Diario de implementación

Una entrada por tarea cerrada o por sesión. Tres líneas: qué se hizo, cómo se comprobó, qué se aprendió.
Lo más reciente arriba.

## 2026-10-29 — Fase 2 cerrada (tareas 2.1 a 2.6)
- Hecho: `Partida` (inventario que vive entre escenas, con moneda, madera, piedra, hierro, hueso y
  pociones), `Regeneracion`, `Fogata`, `FuenteRecurso` (árboles y rocas: 3 golpes, temblor, se agotan
  60 s), `IGolpeable`, `EfectoDano` + `NumeroFlotante`, muerte por escena en `GestorEscena`,
  `BotonContexto`, `Avisos` y `Sonidos` (Kenney RPG Audio e Impact Sounds, 48 clips).
- Comprobado: 7 tests nuevos en Play Mode (golpear árbol → 1 madera, regenerar, fogata, parpadeo,
  muerte en pueblo y en mazmorra, icono del botón, sonidos), 10 de 10 OK; fotos 40-42; APK en el
  emulador a 60 fps.
- Aprendido: tintar con `_Color` multiplica la textura y deja al personaje casi negro; para el
  parpadeo hay que cambiar el material entero. Los objetos marcados estáticos no se pueden mover ni
  escalar en juego: árboles y rocas sueltos no se marcan.

## 2026-10-29 — Fase 1 cerrada (tareas 1.2 a 1.7)
- Hecho: `MontarEscenas` monta Pueblo, Mazmorra1, Fuerte y Asalto desde `pueblo.txt` (48x48),
  `catacumbas.txt` (20x30) y `fuerte-base.txt` (16x16), con prefabs (`CrearPrefabs`), puertas con
  `Transicion` y `GestorEscena`, colisiones unidas por fila y mallas fundidas por trozos (`CombinarMallas`).
  Nature Kit completo y Mini Characters importados. Tests en Play Mode con Unity Test Framework 1.7.0.
- Comprobado: 3 tests de transición OK (mazmorra, fuerte, asalto desde el cofre); fotos de `Revisar`
  de las cuatro escenas; en el emulador se anda de la fogata a la mazmorra con el joystick y entra;
  60 fps estables en Pueblo y en Mazmorra1.
- Aprendido: el emulador tenía 2 GB de RAM e iba a 5-15 fps por memoria de intercambio, incluso con
  una escena vacía a 28 fps; con 4 GB (`hw.ramSize` en `Medium_Phone.avd/config.ini`) da 60 fps.
  Los pulsos de botón deben leerse y borrarse en el mismo `Update` del jugador o se pierden.

## 2026-10-29 — Tarea 1.1: PaletaMapa y CargadorMapa
- Hecho: `MapaTexto` (cabecera + rejilla), `PaletaMapa` (letra → modelos, colisión, bloque NxN, marca),
  `CargadorMapa` (lee y coloca; las letras con lógica propia vuelven como marcas). La paleta se crea
  con `Prototipo > Crear paleta de mapas` en `Assets/Mapas/PaletaMapa.asset`.
- Comprobado: `Prototipo > Probar cargador de mapas` (o batch `PruebaCargadorMapa.EnBatch`) con el
  mapa de ejemplo de la arquitectura y uno interior: 12 de 12 comprobaciones OK y fotos 20 y 21 bien.
  De paso, Unity abre y compila desde la ruta nueva (solo avisos CS0618 que ya existían).
- Añadido después, a petición de Daniel: 34 modelos de río, puente y cascada del Nature Kit 2.1
  (zip de la investigación) en `Assets/Kenney/NatureKit/`. El río elige recto, curva, te, cruce o
  final según sus vecinos; el puente pone el río debajo y se gira para cruzarlo. Prueba: 20 de 20 OK.
- Aprendido: sin `Library`, el primer batch tarda varios minutos. A giro 0, las piezas de río del
  Nature Kit unen: recto norte-sur, curva sur-este, final sur, split oeste-este-sur.

## 2026-10-29 — Fase 0 cerrada
- Hecho: proyecto movido a `C:\AreaDeTrabajo\JUEGOS\orkhold` (sin `Library`, se regenera); repo git;
  README en inglés y español con capturas reales en `docs/img/`; scripts en subcarpetas
  (Entrada, Jugador, Enemigos, Mundo, UI); nombre Orkhold y id `com.orkhold.game`.
- Comprobado: pendiente abrir Unity desde la ruta nueva y confirmar que compila y monta la escena
  (lo hace la primera sesión de la fase 1 antes de empezar).
- Aprendido: el Hub y el cliente de licencias bloquean la carpeta del proyecto aunque Unity esté
  cerrado; copiar con robocopy sin `Library` es lo más rápido.

## 2026-10-29 — Prototipo inicial (antes de la lista de tareas)
- Hecho: proyecto Unity montado a mano; escena PvE generada por `MontarEscenaPvE`; jugador, 6 orcos,
  monedas, pociones, joystick, 3 botones, HUD. APK compilado y probado en el emulador Medium Phone.
- Comprobado: `Prototipo > Revisar` (fotos + Play automático) y capturas del emulador con `adb`.
- Aprendido: ver "Cosas aprendidas" en `CLAUDE.md`.
