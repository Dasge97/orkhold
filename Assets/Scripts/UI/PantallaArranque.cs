using UnityEngine;
using UnityEngine.UI;

/// Pantalla de arranque (01-diseno.md §6.1): logotipo, conexión y crear personaje
/// (4 modelos de Mini Characters y un nombre). Luego, al pueblo.
public class PantallaArranque : MonoBehaviour
{
    [Tooltip("Donde se pone el modelo elegido para verlo.")]
    public Transform pedestal;

    Text _estado, _modeloTexto, _error;
    GameObject _crear;
    InputField _nombre;
    int _elegido;
    GameObject _vista;
    float _giro;

    public enum Fase { Conectando, CrearPersonaje, Entrando }
    public Fase Estado { get; private set; } = Fase.Conectando;

    void Start()
    {
        // Servidor de mundo o bot de pruebas: directos al pueblo, sin pantalla.
        if (RedPueblo.EsServidorDedicado || Configuracion.Bandera("-bot"))
        {
            Partida.Actual.nombre = Configuracion.Argumento("-nombre") ?? (RedPueblo.EsServidorDedicado ? "servidor" : "Bot");
            Partida.Actual.modelo = Configuracion.Argumento("-modelo") ?? "character-male-c";
            Guardado.Activo = false;
            if (RedPueblo.EsServidorDedicado) { Application.targetFrameRate = 30; UnityEngine.SceneManagement.SceneManager.LoadScene("Pueblo"); return; }
            if (!Configuracion.ModoLocal) { Sesion.Iniciar((ok, e) => UnityEngine.SceneManagement.SceneManager.LoadScene("Pueblo")); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("Pueblo");
            return;
        }
        var go = Ui.Lienzo("Arranque", 100);
        go.transform.SetParent(transform, false);
        Ui.Texto(go.transform, "ORKHOLD", 120, new Vector2(0f, 400f), new Vector2(1400f, 160f), negrita: true).color = Ui.Dorado;
        _estado = Ui.Texto(go.transform, "Conectando...", 40, new Vector2(0f, 300f), new Vector2(1400f, 60f));

        _crear = new GameObject("CrearPersonaje", typeof(RectTransform));
        _crear.transform.SetParent(go.transform, false);
        var rt = (RectTransform)_crear.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Ui.Texto(_crear.transform, "Crea tu personaje", 52, new Vector2(-520f, 180f), new Vector2(700f, 70f), negrita: true);
        Ui.Boton(_crear.transform, "<", new Vector2(-760f, 40f), new Vector2(120f, 120f), Ui.Gris, () => Elegir(_elegido - 1), 60);
        Ui.Boton(_crear.transform, ">", new Vector2(-280f, 40f), new Vector2(120f, 120f), Ui.Gris, () => Elegir(_elegido + 1), 60);
        _modeloTexto = Ui.Texto(_crear.transform, "", 40, new Vector2(-520f, 40f), new Vector2(340f, 120f));
        _nombre = Ui.Entrada(_crear.transform, "Tu nombre", new Vector2(-520f, -120f), new Vector2(620f, 100f), 16);
        Ui.Boton(_crear.transform, "Empezar", new Vector2(-520f, -270f), new Vector2(420f, 110f), Ui.Verde, Empezar, 48);
        _error = Ui.Texto(_crear.transform, "", 34, new Vector2(-520f, -380f), new Vector2(900f, 60f));
        _error.color = new Color(1f, 0.55f, 0.5f);
        _crear.SetActive(false);

        Conectar();
    }

    void Update()
    {
        if (_vista) { _giro += 40f * Time.deltaTime; _vista.transform.rotation = Quaternion.Euler(0f, 180f + Mathf.Sin(_giro * Mathf.Deg2Rad) * 35f, 0f); }
    }

    void Conectar()
    {
        if (Configuracion.ModoLocal)
        {
            _estado.text = "Modo local (sin servidor)";
            Siguiente();
            return;
        }
        Sesion.Iniciar((ok, error) =>
        {
            if (!ok)
            {
                _estado.text = "Sin conexión: juegas sin servidor (fuerte y asalto bloqueados)";
                Siguiente();
                return;
            }
            _estado.text = "Conectado";
            Sincronizacion.Refrescar(_ => Siguiente());
        });
    }

    void Siguiente()
    {
        if (string.IsNullOrEmpty(Partida.Actual.nombre)) MostrarCrear();
        else Entrar();
    }

    void MostrarCrear()
    {
        Estado = Fase.CrearPersonaje;
        _crear.SetActive(true);
        Elegir(0);
    }

    public void Elegir(int i)
    {
        var cat = Catalogo.Instancia;
        int n = cat.idsPersonajes.Length;
        _elegido = ((i % n) + n) % n;
        _modeloTexto.text = cat.nombresPersonajes[_elegido];
        if (_vista) Destroy(_vista);
        _vista = Instantiate(cat.modelosPersonajes[_elegido], pedestal ? pedestal.position : Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        _vista.transform.localScale = Vector3.one * cat.escalaPersonajes * 2.2f;
        var an = _vista.GetComponentInChildren<Animator>();
        if (!an) an = _vista.AddComponent<Animator>();   // los FBX de Kenney no siempre lo traen
        an.runtimeAnimatorController = cat.controladoresPersonajes[_elegido];
        an.applyRootMotion = false;
    }

    /// Crea el personaje con el nombre escrito (también lo usan los tests).
    public void Empezar() => Empezar(_nombre.text);

    public void Empezar(string nombre)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length < 3) { _error.text = "El nombre necesita al menos 3 letras"; return; }
        string modelo = Catalogo.Instancia.idsPersonajes[_elegido];
        if (!Sesion.Conectado)
        {
            Partida.Actual.nombre = nombre; Partida.Actual.modelo = modelo; Partida.Cambiado();
            Entrar();
            return;
        }
        _error.text = "";
        ApiCliente.Put("/jugador/personaje", new MensajesApi.PeticionPersonaje { nombre = nombre, modelo = modelo }, r =>
        {
            if (!r.Ok) { _error.text = r.Mensaje; return; }
            Sincronizacion.AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador);
            Entrar();
        });
    }

    void Entrar()
    {
        Estado = Fase.Entrando;
        _crear.SetActive(false);
        _estado.text = $"¡Hola, {Partida.Actual.nombre}!";
        Viaje.llegarA = "";
        Transicion.Ir("Pueblo");
    }
}
