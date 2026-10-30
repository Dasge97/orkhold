using System;
using UnityEngine;

/// Sesión con la API (14-cuentas.md §1 y §5).
/// - Cuenta invitada: token del dispositivo en PlayerPrefs (POST /sesion), como en la primera versión.
/// - Cuenta registrada: correo y contraseña (POST /cuenta/entrar o /cuenta/registro). El token de sesión
///   se guarda en el móvil y vale 30 días; al caducar se vuelve a pedir la contraseña.
public static class Sesion
{
    const string ClaveDispositivo = "token_dispositivo";
    const string ClaveToken = "token_sesion";
    const string ClaveRegistrada = "cuenta_registrada";

    public static string Token { get; private set; } = "";
    public static string JugadorId { get; private set; } = "";
    /// Hay sesión con la API (en modo local siempre es falso).
    public static bool Conectado => !string.IsNullOrEmpty(Token);
    /// Hay servidor configurado pero no se pudo conectar: fuerte y saqueo bloqueados.
    public static bool SinConexion => !Configuracion.ModoLocal && !Conectado;
    /// Este móvil entró con una cuenta registrada (si la sesión caduca, hay que volver a entrar).
    public static bool Registrada => PlayerPrefs.GetInt(ClaveRegistrada, 0) == 1;
    /// La última vez que se intentó entrar hacía falta correo y contraseña.
    public static bool PideEntrar { get; private set; }
    /// Código de error de la API en el último intento de entrar (p. ej. "cuenta_bloqueada").
    public static string UltimoError { get; private set; } = "";

    public static MensajesApi.Cuenta Cuenta { get; private set; }

    // Mientras se comprueba la sesión guardada, un 401 lo resuelve Iniciar (no Caducada).
    static bool _comprobando;

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

    /// Entra con la sesión guardada o, si no hay, con la invitada del dispositivo.
    /// "fin(true)" si hay sesión; si hace falta correo y contraseña, fin(false) y PideEntrar.
    public static void Iniciar(Action<bool, string> fin)
    {
        PideEntrar = false;
        UltimoError = "";
        if (Configuracion.ModoLocal) { fin?.Invoke(false, "Modo local"); return; }
        var guardado = PlayerPrefs.GetString(ClaveToken, "");
        if (!string.IsNullOrEmpty(guardado))
        {
            Token = guardado;
            _comprobando = true;
            ApiCliente.Get("/jugador", r =>
            {
                _comprobando = false;
                if (r.Ok) { Aceptar(guardado, r.Leer<MensajesApi.RespuestaJugador>().jugador); fin?.Invoke(true, ""); return; }
                if (r.codigo == 0) { Token = ""; AlCambiar?.Invoke(); fin?.Invoke(false, r.Mensaje); return; }
                Olvidar();
                UltimoError = r.CodigoError;
                if (r.CodigoError == "cuenta_bloqueada" || Registrada) { PideEntrar = r.CodigoError != "cuenta_bloqueada"; fin?.Invoke(false, r.Mensaje); return; }
                Invitada(fin);
            });
            return;
        }
        if (Registrada) { PideEntrar = true; fin?.Invoke(false, "Entra con tu correo y contraseña"); return; }
        Invitada(fin);
    }

    static void Invitada(Action<bool, string> fin)
    {
        ApiCliente.Post("/sesion", new MensajesApi.PeticionSesion { token_dispositivo = TokenDispositivo }, r =>
        {
            if (!r.Ok)
            {
                Token = ""; AlCambiar?.Invoke();
                UltimoError = r.CodigoError;
                PideEntrar = r.CodigoError == "cuenta_registrada";
                fin?.Invoke(false, r.Mensaje);
                return;
            }
            var s = r.Leer<MensajesApi.RespuestaSesion>();
            PlayerPrefs.SetInt(ClaveRegistrada, 0);
            Aceptar(s.token, s.jugador);
            fin?.Invoke(true, "");
        });
    }

    /// POST /cuenta/entrar.
    public static void Entrar(string correo, string contrasena, Action<bool, string> fin)
    {
        ApiCliente.Post("/cuenta/entrar", new MensajesApi.PeticionCuenta { correo = correo, contrasena = contrasena }, r => Recibir(r, fin));
    }

    /// POST /cuenta/registro: con sesión de invitada la convierte; sin sesión crea una cuenta nueva.
    public static void Registrar(string correo, string contrasena, Action<bool, string> fin)
    {
        ApiCliente.Post("/cuenta/registro", new MensajesApi.PeticionCuenta { correo = correo, contrasena = contrasena }, r => Recibir(r, fin));
    }

    static void Recibir(ApiCliente.Respuesta r, Action<bool, string> fin)
    {
        if (!r.Ok) { fin?.Invoke(false, r.Mensaje); return; }
        var s = r.Leer<MensajesApi.RespuestaCuenta>();
        PlayerPrefs.SetInt(ClaveRegistrada, 1);
        Cuenta = s.cuenta;
        Aceptar(s.token, s.jugador);
        fin?.Invoke(true, "");
    }

    /// GET /cuenta: tipo, correo y si está verificado.
    public static void LeerCuenta(Action<MensajesApi.Cuenta> fin)
    {
        ApiCliente.Get("/cuenta", r =>
        {
            Cuenta = r.Ok ? r.Leer<MensajesApi.RespuestaCuentaSola>().cuenta : Cuenta;
            fin?.Invoke(r.Ok ? Cuenta : null);
        });
    }

    /// Cierra la sesión de este móvil. Después se puede entrar con otra cuenta o jugar como invitado.
    public static void Salir(Action fin)
    {
        ApiCliente.Post("/cuenta/salir", null, _ =>
        {
            Olvidar();
            PlayerPrefs.SetInt(ClaveRegistrada, 1);   // la invitada de este móvil ya tiene correo: hay que entrar
            fin?.Invoke();
        });
    }

    /// DELETE /cuenta. Si es registrada, con la contraseña; si es invitada, "confirmar" = "BORRAR".
    public static void Borrar(string contrasena, string confirmar, Action<bool, string> fin)
    {
        ApiCliente.Borrar("/cuenta", new MensajesApi.PeticionBorrar { contrasena = contrasena, confirmar = confirmar }, r =>
        {
            if (!r.Ok) { fin?.Invoke(false, r.Mensaje); return; }
            Olvidar();
            // Empieza de cero: nuevo dispositivo y nueva invitada al volver a entrar.
            PlayerPrefs.DeleteKey(ClaveDispositivo);
            PlayerPrefs.SetInt(ClaveRegistrada, 0);
            fin?.Invoke(true, "");
        });
    }

    /// "Jugar como invitado" desde la pantalla de entrar: una invitada nueva en este móvil.
    public static void NuevaInvitada(Action<bool, string> fin)
    {
        Olvidar();
        PlayerPrefs.DeleteKey(ClaveDispositivo);
        PlayerPrefs.SetInt(ClaveRegistrada, 0);
        Invitada(fin);
    }

    static void Aceptar(string token, MensajesApi.Jugador j)
    {
        Token = token;
        JugadorId = j?.id ?? "";
        PlayerPrefs.SetString(ClaveToken, token);
        PlayerPrefs.Save();
        Sincronizacion.AplicarJugador(j);
        AlCambiar?.Invoke();
    }

    static void Olvidar()
    {
        Token = ""; JugadorId = ""; Cuenta = null;
        PlayerPrefs.DeleteKey(ClaveToken);
        PlayerPrefs.Save();
        AlCambiar?.Invoke();
    }

    /// La API dijo 401: la sesión no vale. Se vuelve a pedir (o a la pantalla de entrar si es registrada).
    public static void Caducada()
    {
        if (_comprobando) return;
        Olvidar();
        if (!Registrada) Iniciar(null);
        else PideEntrar = true;
    }

    /// Para los tests.
    public static void Cerrar() { Token = ""; JugadorId = ""; Cuenta = null; PlayerPrefs.DeleteKey(ClaveToken); AlCambiar?.Invoke(); }

    /// Para los tests: olvida todo lo guardado (dispositivo, sesión y si era registrada).
    public static void Reiniciar()
    {
        Cerrar();
        PlayerPrefs.DeleteKey(ClaveDispositivo);
        PlayerPrefs.DeleteKey(ClaveRegistrada);
    }
}
