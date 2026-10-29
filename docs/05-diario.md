# 05 · Diario de implementación

Una entrada por tarea cerrada o por sesión. Tres líneas: qué se hizo, cómo se comprobó, qué se aprendió.
Lo más reciente arriba.

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
