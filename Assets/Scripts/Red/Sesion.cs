using System;
using UnityEngine;

/// Sesión con la API (02-arquitectura.md §2.6): token de dispositivo en PlayerPrefs (sin contraseña
/// en la primera versión) y token de sesión que devuelve POST /sesion.
public static class Sesion
{
    const string ClaveDispositivo = "token_dispositivo";

    public static string Token { get; private set; } = "";
    public static string JugadorId { get; private set; } = "";
    /// Hay sesión con la API (en modo local siempre es falso).
    public static bool Conectado => !string.IsNullOrEmpty(Token);
    /// Hay servidor configurado pero no se pudo conectar: fuerte y asalto bloqueados.
    public static bool SinConexion => !Configuracion.ModoLocal && !Conectado;

    public static event Action AlCambiar;

    public static string TokenDispositivo
    {
        get
        {
            var t = PlayerPrefs.GetString(ClaveDispositivo, "");
            if (string.IsNullOrEmpty(t))
            {
                t = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(ClaveDispositivo, t);
                PlayerPrefs.Save();
            }
            return t;
        }
    }

    /// POST /sesion. "fin(true)" si hay sesión; los datos del jugador ya están aplicados a la partida.
    public static void Iniciar(Action<bool, string> fin)
    {
        if (Configuracion.ModoLocal) { fin?.Invoke(false, "Modo local"); return; }
        ApiCliente.Post("/sesion", new MensajesApi.PeticionSesion { token_dispositivo = TokenDispositivo }, r =>
        {
            if (!r.Ok) { Token = ""; AlCambiar?.Invoke(); fin?.Invoke(false, r.Mensaje); return; }
            var s = r.Leer<MensajesApi.RespuestaSesion>();
            Token = s.token;
            JugadorId = s.jugador.id;
            Sincronizacion.AplicarJugador(s.jugador);
            AlCambiar?.Invoke();
            fin?.Invoke(true, "");
        });
    }

    /// La API dijo 401: la sesión no vale. Se vuelve a pedir la próxima vez.
    public static void Caducada()
    {
        Token = "";
        AlCambiar?.Invoke();
        Iniciar(null);
    }

    /// Para los tests.
    public static void Cerrar() { Token = ""; JugadorId = ""; AlCambiar?.Invoke(); }
}
