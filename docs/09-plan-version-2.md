# 09 · Plan de la versión 2

Fecha: 2026-10-30. Lo que Daniel decidió el 30-10-2026 sobre lo que falta por definir. Cada bloque
tendrá su documento de diseño por escrito **antes** de que el modelo lo implemente. Aquí solo se dice
qué es cada bloque, qué decisión ya está tomada y en qué orden van.

| # | Bloque | Decisión tomada | Documento | Estado |
|---|---|---|---|---|
| 1 | Saqueo (PvP nuevo) | Esconder recursos, acciones contadas, el servidor decide | `08-formato-y-pvp.md` | Aprobado el 30-10-2026 |
| 2 | Balance fuera del código y panel de administración | Los números del juego se ajustan sin tocar código, desde un panel web con más cosas | `10-panel-admin.md` (por escribir) | Por diseñar |
| 3 | Mazmorras 2, 3 y 4 | Diseñadas por escrito (plano, enemigos, jefe, qué desbloquean) para que el modelo las pinte e implemente | `11-mazmorras.md` (por escribir) | Por diseñar |
| 4 | Tutorial guiado | Un minitutorial paso a paso en los primeros minutos | — | Más adelante |
| 5 | Eventos del pueblo | 2-3 eventos diseñados ahora; más con el tiempo | `12-eventos.md` (por escribir) | Por diseñar |
| 6 | Social: chat, amigos, gremios | Todo tiene que existir. Es un MMORPG al uso con el método de juego de Orkhold | `13-social.md` (por escribir) | Por diseñar |
| 7 | Cuentas con registro | La cuenta **no** va atada al móvil. Registro y entrada por el backend | `14-cuentas.md` (por escribir) | Por diseñar |
| 8 | Sonido | No preocupa ahora | — | Más adelante |
| 9 | Landing y web en orkhold.com | Página profesional con sus pestañas, al nivel de la web de un juego grande | Proyecto aparte | Por diseñar |
| 10 | Play Store | Cuando todo esté atado | — | Al final |

## 2. Panel de administración y balance (bloque 2)

Qué tiene que poder hacer, sin tocar código:
- **Balance**: ver y editar todos los números del juego (costes, vidas, capacidades, acciones,
  porcentajes, tiempos, límites por día). Cada número con nombre, valor, valor por defecto y la fecha
  del último cambio. El cliente descarga la tabla al arrancar y el servidor de mundo también.
- **Historial de cambios de balance**: quién cambió qué y cuándo, para volver atrás.
- **Jugadores**: buscar, ver su fuerte, inventario, saqueos, puntuación. Bloquear, cambiar nombre.
- **Saqueos**: lista de los últimos, filtrar por sospechosos (demasiados aciertos, tiempos imposibles).
- **Eventos**: programar un evento (bloque 5) con fecha y parámetros.
- **Estado del servidor**: jugadores conectados, versión del cliente mínima, mensaje de mantenimiento.
- **Avisos**: mandar un aviso a todos los jugadores.

Dónde vive: en la misma API (Fastify) con rutas `/admin/...` protegidas, y una web aparte (Vue o
similar) desplegada en codehive. Las credenciales de admin en la bóveda.

## 7. Cuentas con registro (bloque 7)

Decidido: registro real que pasa por el backend. Por definir en `14-cuentas.md`:
- Registro con correo y contraseña. Verificación del correo (enlace). Recuperar contraseña.
- Entrada con correo y contraseña, o con Google (más adelante).
- Una cuenta, varios móviles. Sesiones con token que caduca.
- Nombre de personaje único, con filtro de nombres ofensivos.
- Borrar cuenta (obligatorio en Play Store).
- Lo que hay hoy (cuenta por dispositivo) pasa a ser una cuenta "invitada" que se puede convertir en
  cuenta registrada sin perder nada.

## 9. Web en orkhold.com (bloque 9)

Decisiones de Daniel del 30-10-2026:
| Tema | Decisión |
|---|---|
| Qué es | La web de un MMO clásico: ancho completo, secciones bien definidas, al nivel de la web de un juego grande. Tomar referencias de webs de MMO antes de diseñar. |
| Idiomas | Castellano e inglés desde el principio. |
| Cuenta | La web permite registrarse, entrar, ver el perfil propio y el fuerte propio. Misma cuenta que el juego (`14-cuentas.md`). |
| Contenido vivo | Ranking, noticias y eventos salen de la API en tiempo real. |
| Tecnología | La elijo yo: **Nuxt 3** (Vue con servidor, Node 22) con `@nuxtjs/i18n`, en Docker en codehive detrás de Traefik con HTTPS, como el resto. |
| Dominio | orkhold.com ya está comprado. Si hace falta un subdominio (`api.orkhold.com`, `mundo.orkhold.com`, `admin.orkhold.com`), se le pide a Daniel. |
| Marca | Logotipo, colores y tipografía se cierran al final. La temática ya está clara (orcos, valle, Vado Gris). |
| Secciones | Inicio, El juego, Mundo (lore y personajes de `07-lore.md`), Noticias, Eventos, Ranking, Descargar, Soporte, Cuenta, Legal (privacidad y términos). |

Proyecto aparte (`C:\AreaDeTrabajo\JUEGOS\orkhold-web`), con la skill `diseno-web`. Se diseña cuando
el saqueo y las cuentas estén cerrados, para que la web no cuente algo que luego cambia. Tendrá su
propio documento de diseño con el contenido de cada sección antes de escribir código.

## Orden propuesto

1. Aprobar el saqueo (`08`) y escribir `10-panel-admin.md` con la tabla de balance: el saqueo ya se
   implementa leyendo los números de ahí.
2. `14-cuentas.md`: el saqueo mueve recursos reales entre cuentas, así que las cuentas tienen que estar bien antes.
3. Implementar saqueo + balance + cuentas (fases 10, 11 y 12 de `03-tareas.md`).
4. `11-mazmorras.md` y `12-eventos.md`; implementar.
5. `13-social.md`; implementar.
6. Tutorial guiado.
7. Web en orkhold.com.
8. Play Store.
