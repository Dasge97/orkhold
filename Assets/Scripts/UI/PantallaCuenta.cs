using System;
using UnityEngine;
using UnityEngine.UI;

/// Cuenta del jugador (14-cuentas.md §5), en una ventana encima de todo:
/// - Entrar: correo y contraseña, "He olvidado la contraseña" y "Jugar como invitado".
/// - Cuenta: invitada → "Guardar mi progreso" con correo y contraseña; registrada → reenviar el correo de
///   confirmación y cerrar sesión. Las dos: borrar la cuenta (con contraseña, o escribiendo BORRAR).
public class PantallaCuenta : MonoBehaviour
{
    public enum Modo { Entrar, Cuenta, Borrar }

    public static PantallaCuenta Abierta { get; private set; }

    Modo _modo;
    Action _alTerminar;
    RectTransform _panel;
    GameObject _velo;
    Text _mensaje;
    InputField _correo, _clave;
    bool _ocupada;

    public Modo ModoActual => _modo;
    public string Mensaje => _mensaje ? _mensaje.text : "";

    /// Abre la ventana. "alTerminar" se llama al entrar, al guardar la cuenta o al cerrar la ventana.
    public static PantallaCuenta Abrir(Modo modo, Action alTerminar = null)
    {
        if (Abierta) Destroy(Abierta.gameObject);
        var go = Ui.Lienzo("PantallaCuenta", 950);
        var p = go.AddComponent<PantallaCuenta>();
        p._alTerminar = alTerminar;
        Abierta = p;
        p.Mostrar(modo);
        return p;
    }

    void OnDestroy() { if (Abierta == this) Abierta = null; }

    public void Cerrar()
    {
        var fin = _alTerminar;
        Destroy(gameObject);
        fin?.Invoke();
    }

    void Mostrar(Modo modo)
    {
        _modo = modo;
        if (_velo) Destroy(_velo);
        _panel = Ui.PanelCentrado(transform, new Vector2(1100f, 900f), out _velo);
        _correo = _clave = null;
        switch (modo)
        {
            case Modo.Entrar: MontarEntrar(); break;
            case Modo.Cuenta: MontarCuenta(); break;
            case Modo.Borrar: MontarBorrar(); break;
        }
    }

    // ---------- entrar ----------

    void MontarEntrar()
    {
        Titulo("Entra con tu cuenta", "Tu progreso te espera en cualquier móvil.");
        _correo = Campo("Correo", 170f, false);
        _clave = Campo("Contraseña", 50f, true);
        Ui.Boton(_panel, "Entrar", new Vector2(0f, -80f), new Vector2(560f, 110f), Ui.Verde, Entrar, 46);
        Ui.Boton(_panel, "He olvidado la contraseña", new Vector2(-280f, -210f), new Vector2(520f, 90f), Ui.Gris, Olvide, 32);
        // Con una invitada ya en marcha (se abrió desde crear personaje), volver; si no, empezar una invitada nueva.
        if (Sesion.Conectado) Ui.Boton(_panel, "Volver", new Vector2(280f, -210f), new Vector2(520f, 90f), Ui.Gris, Cerrar, 32);
        else Ui.Boton(_panel, "Jugar como invitado", new Vector2(280f, -210f), new Vector2(520f, 90f), Ui.Gris, Invitado, 32);
        _mensaje = Ui.Texto(_panel, "", 34, new Vector2(0f, -340f), new Vector2(1000f, 120f));
    }

    public void Entrar() => Entrar(_correo.text, _clave.text);

    public void Entrar(string correo, string clave)
    {
        if (!Comprobar(correo, clave)) return;
        Ocupar("Entrando...");
        Sesion.Entrar(correo.Trim(), clave, (ok, error) =>
        {
            _ocupada = false;
            if (!ok) { Error(error); return; }
            Sincronizacion.Refrescar(_ => Cerrar());
        });
    }

    void Olvide()
    {
        var correo = _correo.text.Trim();
        if (!correo.Contains("@")) { Error("Escribe tu correo y vuelve a pulsar"); return; }
        Ocupar("Enviando...");
        ApiCliente.Post("/cuenta/olvide", new MensajesApi.PeticionCorreo { correo = correo }, r =>
        {
            _ocupada = false;
            if (r.Ok) Bien("Si ese correo tiene una cuenta confirmada, te hemos mandado un enlace para poner otra contraseña");
            else Error(r.Mensaje);
        });
    }

    void Invitado()
    {
        Ocupar("Entrando...");
        Sesion.NuevaInvitada((ok, error) =>
        {
            _ocupada = false;
            if (!ok) { Error(error); return; }
            Cerrar();
        });
    }

    // ---------- cuenta ----------

    void MontarCuenta()
    {
        Titulo("Tu cuenta", "Cargando...");
        Sesion.LeerCuenta(c =>
        {
            if (this == null || _modo != Modo.Cuenta) return;
            if (_velo) Destroy(_velo);
            _panel = Ui.PanelCentrado(transform, new Vector2(1100f, 900f), out _velo);
            if (c == null)
            {
                Titulo("Tu cuenta", "Sin conexión con el servidor. Prueba otra vez en un rato.");
                Ui.Boton(_panel, "Cerrar", new Vector2(0f, -290f), new Vector2(560f, 90f), Ui.Gris, Cerrar, 34);
                return;
            }
            if (c.tipo == "invitada")
            {
                Titulo("Guarda tu progreso", "Ahora es una cuenta invitada: si pierdes el móvil, pierdes tu fuerte.\nCon un correo juegas en cualquier móvil.");
                _correo = Campo("Correo", 140f, false);
                _clave = Campo("Contraseña (8 o más)", 25f, true);
                Ui.Boton(_panel, "Guardar mi progreso", new Vector2(0f, -90f), new Vector2(560f, 100f), Ui.Verde, Registrar, 42);
                Ui.Boton(_panel, "Entrar con otra cuenta", new Vector2(-280f, -210f), new Vector2(520f, 90f), Ui.Gris, () => Mostrar(Modo.Entrar), 32);
            }
            else
            {
                Titulo("Tu cuenta", c.correo + (c.verificado ? "\nCorreo confirmado" : "\nCorreo sin confirmar: mira tu bandeja de entrada"));
                if (!c.verificado) Ui.Boton(_panel, "Reenviar el correo", new Vector2(0f, 120f), new Vector2(560f, 90f), Ui.Gris, Reenviar, 34);
                Ui.Boton(_panel, "Cerrar sesión", new Vector2(-280f, -210f), new Vector2(520f, 90f), Ui.Gris, Salir, 34);
            }
            Ui.Boton(_panel, "Borrar cuenta", new Vector2(280f, -210f), new Vector2(520f, 90f), Ui.Rojo, () => Mostrar(Modo.Borrar), 34);
            Ui.Boton(_panel, "Cerrar", new Vector2(0f, -320f), new Vector2(560f, 90f), Ui.Gris, Cerrar, 34);
            _mensaje = Ui.Texto(_panel, "", 34, new Vector2(0f, -405f), new Vector2(1000f, 70f));
        });
    }

    public void Registrar() => Registrar(_correo.text, _clave.text);

    public void Registrar(string correo, string clave)
    {
        if (!Comprobar(correo, clave)) return;
        if (clave.Length < 8) { Error("La contraseña necesita 8 caracteres o más"); return; }
        Ocupar("Guardando...");
        Sesion.Registrar(correo.Trim(), clave, (ok, error) =>
        {
            _ocupada = false;
            if (!ok) { Error(error); return; }
            Avisos.Mostrar("Progreso guardado. Te hemos mandado un correo para confirmarlo", 3f);
            Cerrar();
        });
    }

    void Reenviar()
    {
        Ocupar("Enviando...");
        ApiCliente.Post("/cuenta/verificar", null, r => { _ocupada = false; if (r.Ok) Bien("Correo enviado"); else Error(r.Mensaje); });
    }

    void Salir()
    {
        Ocupar("Cerrando sesión...");
        Sesion.Salir(() => { Destroy(gameObject); Transicion.Ir("Arranque"); });
    }

    // ---------- borrar ----------

    void MontarBorrar()
    {
        bool registrada = Sesion.Cuenta != null && Sesion.Cuenta.tipo == "registrada";
        Titulo("Borrar la cuenta", "Se borran tu personaje, tu fuerte y todo lo que tienes. No se puede deshacer.");
        _clave = Campo(registrada ? "Tu contraseña" : "Escribe BORRAR", 200f, registrada);
        Ui.Boton(_panel, "Borrar para siempre", new Vector2(0f, 40f), new Vector2(560f, 100f), Ui.Rojo, () => Borrar(_clave.text), 40);
        Ui.Boton(_panel, "Volver", new Vector2(0f, -90f), new Vector2(560f, 90f), Ui.Gris, () => Mostrar(Modo.Cuenta), 34);
        _mensaje = Ui.Texto(_panel, "", 34, new Vector2(0f, -220f), new Vector2(1000f, 120f));
    }

    public void Borrar(string texto)
    {
        bool registrada = Sesion.Cuenta != null && Sesion.Cuenta.tipo == "registrada";
        Ocupar("Borrando...");
        Sesion.Borrar(registrada ? texto : "", registrada ? "" : texto.Trim().ToUpperInvariant(), (ok, error) =>
        {
            _ocupada = false;
            if (!ok) { Error(error); return; }
            Partida.Reemplazar(new DatosPartida());
            Guardado.Guardar();
            Destroy(gameObject);
            Transicion.Ir("Arranque");
        });
    }

    // ---------- piezas ----------

    void Titulo(string titulo, string texto)
    {
        Ui.Texto(_panel, titulo, 58, new Vector2(0f, 380f), new Vector2(1000f, 80f), negrita: true).color = Ui.Dorado;
        if (!string.IsNullOrEmpty(texto)) Ui.Texto(_panel, texto, 32, new Vector2(0f, 285f), new Vector2(1000f, 110f));
    }

    InputField Campo(string ayuda, float y, bool clave)
    {
        var c = Ui.Entrada(_panel, ayuda, new Vector2(0f, y), new Vector2(760f, 100f), 128);
        if (clave) { c.contentType = InputField.ContentType.Password; c.inputType = InputField.InputType.Password; }
        else c.contentType = InputField.ContentType.EmailAddress;
        return c;
    }

    bool Comprobar(string correo, string clave)
    {
        if (_ocupada) return false;
        if (string.IsNullOrWhiteSpace(correo) || !correo.Contains("@")) { Error("Escribe tu correo"); return false; }
        if (string.IsNullOrEmpty(clave)) { Error("Escribe la contraseña"); return false; }
        return true;
    }

    void Ocupar(string texto) { _ocupada = true; if (_mensaje) { _mensaje.color = Color.white; _mensaje.text = texto; } }
    void Error(string texto) { if (_mensaje) { _mensaje.color = new Color(1f, 0.55f, 0.5f); _mensaje.text = texto; } }
    void Bien(string texto) { if (_mensaje) { _mensaje.color = new Color(0.6f, 0.95f, 0.6f); _mensaje.text = texto; } }
}
