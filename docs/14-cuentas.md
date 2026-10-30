# 14 · Cuentas con registro

Fecha: 2026-10-30. Bloque 7 de `09-plan-version-2.md`. Decisión de Daniel: la cuenta **no** va atada al
móvil; registro y entrada por el backend. El resto lo propone el modelo.

## 1. Tipos de cuenta
| Tipo | Cómo nace | Qué puede hacer |
|---|---|---|
| Invitada | Al abrir el juego por primera vez sin registrarse (como hoy: token del dispositivo) | Jugar todo. Si se pierde el móvil, se pierde la cuenta. |
| Registrada | Registro con correo y contraseña, o convertir la invitada | Jugar en varios móviles y en la web. Recuperar la contraseña. |

- Convertir la invitada en registrada no pierde nada: es la misma fila de `jugadores` con correo y contraseña.
- Una cuenta registrada deja de depender del token del dispositivo. Ese móvil entra sin pedir nada mientras
  su sesión dure; otro móvil entra con correo y contraseña.
- Si en un móvil con invitada se entra en una cuenta registrada, la invitada queda en el servidor sin tocar
  (se puede volver a ella cerrando sesión) y el juego avisa antes.

## 2. Reglas
| Tema | Regla |
|---|---|
| Correo | Único, sin distinguir mayúsculas. Se guarda en minúsculas. |
| Contraseña | De 8 a 128 caracteres. Se guarda con scrypt y sal propia. Nunca se devuelve ni se registra. |
| Verificación | Al registrarse llega un correo con un enlace (48 h). Sin verificar se juega igual; verificar hace falta para recuperar la contraseña. |
| Recuperar | "He olvidado la contraseña" manda un enlace (1 h) a una página de la API para poner otra. Cierra las demás sesiones. |
| Sesiones | Token firmado de 30 días, ligado a una fila de `sesiones`. Cerrar sesión o cambiar la contraseña anula sesiones. |
| Intentos | 5 entradas fallidas seguidas con un correo: 15 minutos sin poder probar ese correo. |
| Nombre de personaje | Único (ya lo es). De 3 a 16 caracteres. Filtro de palabras ofensivas (lista en `api/src/datos/nombres-prohibidos.txt`, comparando sin tildes ni mayúsculas y quitando números puestos como letras: 0→o, 1→i, 3→e, 4→a, 5→s, 7→t). |
| Borrar cuenta | Desde el juego y la web, con la contraseña (o confirmando dos veces si es invitada). Borra jugador, fuerte, inventario y sesiones; en los saqueos que hizo o recibió queda "Cuenta borrada". Obligatorio en Play Store. |
| Bloqueo | El panel puede bloquear una cuenta: no puede iniciar sesión y las sesiones abiertas dejan de valer. |
| Google | Más adelante. La tabla deja sitio (`google_id`). |

## 3. Correo
- La API manda los correos por SMTP con la configuración `SMTP_URL` (p. ej. `smtps://usuario:clave@servidor:465`)
  y el remitente `CORREO_REMITENTE` (`Orkhold <no-responder@orkhold.com>`).
- Sin `SMTP_URL`, el enlace se escribe en el registro de la API (para pruebas y hasta que haya buzón).
- Los enlaces apuntan a `https://app.orkhold.com/cuenta/...`: páginas sencillas servidas por la API.

## 4. API
| Ruta | Qué |
|---|---|
| `POST /sesion` | Como hoy (invitada por token de dispositivo). Si esa invitada ya se registró, responde `cuenta_registrada` y el juego pide entrar. |
| `POST /cuenta/registro` | Con sesión de invitada: correo y contraseña → la convierte. Sin sesión: crea una cuenta nueva. |
| `POST /cuenta/entrar` | Correo y contraseña (+ token del dispositivo) → token de sesión. |
| `POST /cuenta/salir` | Anula la sesión actual. |
| `POST /cuenta/verificar` y `GET /cuenta/verificar?t=` | Reenviar el correo; página del enlace. |
| `POST /cuenta/olvide` y `GET/POST /cuenta/restablecer?t=` | Pedir enlace; página para poner contraseña nueva. |
| `PUT /cuenta/contrasena` | Cambiar con la actual. |
| `GET /cuenta` | Correo, verificado, tipo, creada. |
| `DELETE /cuenta` | Borrar (contraseña o `confirmar: "BORRAR"` si es invitada). |

## 5. En el juego
- Pantalla de arranque: "Jugar" (invitada o la sesión guardada), "Entrar con mi cuenta".
- Menú: sección "Cuenta" con el estado (invitada o el correo), "Guardar mi progreso" (registrar),
  "Cerrar sesión", "Borrar cuenta".
- Aviso al jugador invitado tras su primer saqueo: "Guarda tu progreso con un correo".
