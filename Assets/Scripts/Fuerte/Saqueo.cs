using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// El saqueo en el fuerte del rival (08-formato-y-pvp.md §4). Lo empieza PantallaPrepararSaqueo (que ya ha
/// pagado la tasa con POST /saqueo/empezar) y lo decide el servidor:
/// 1. Explorar (balance: 120 s): moverse, romper muros a golpes y mirar (1 s parado; responde la API).
///    Cada 0,25 s se apunta la posición y cada golpe a un muro, para que el servidor rehaga la exploración.
///    Trampas y defensores están quietos en esta primera versión (08 §3.3).
/// 2. Marcar: la cámara sube y se tocan hasta "acciones" casillas.
/// 3. Resolver: POST /saqueo/resolver; secuencia corta con lo que salió en cada marca y pantalla de resultado.
[RequireComponent(typeof(Fuerte))]
public class Saqueo : MonoBehaviour
{
    public enum Fase { Sin, Explorar, Mirando, Marcar, Resolviendo, Terminado }

    /// Lo que devolvió POST /saqueo/empezar y el rival de "preparar" (los pone PantallaPrepararSaqueo).
    public static MensajesApi.RespuestaEmpezar Datos;
    public static MensajesApi.RivalSaqueo Rival;

    public static Saqueo Actual { get; private set; }
    public static event Action<MensajesApi.RespuestaResolver> AlTerminar;

    public Fase Estado { get; private set; } = Fase.Sin;
    public float Duracion => Datos != null ? Datos.exploracion_segundos : 120f;
    public float Restante => Mathf.Max(0f, Duracion - _t);
    public float Transcurrido => _t;
    public int Acciones => Datos != null ? Datos.acciones : 3;
    public IReadOnlyList<MensajesApi.Casilla> Marcas => _marcas;
    public int NumEntradas => _entradas.Count;
    public string UltimaMirada { get; private set; } = "";
    public string Error { get; private set; } = "";
    public MensajesApi.RespuestaResolver Resultado { get; private set; }
    public bool PuedeMirar => Estado == Fase.Explorar;
    public Fuerte Fuerte => _fuerte;

    const float CadaEntrada = 0.25f;

    Fuerte _fuerte;
    ControlJugador _jugador;
    PantallaSaqueo _ui;
    float _t, _siguienteEntrada;
    readonly List<MensajesApi.EntradaSaqueo> _entradas = new List<MensajesApi.EntradaSaqueo>();
    readonly List<MensajesApi.Casilla> _marcas = new List<MensajesApi.Casilla>();
    readonly Dictionary<Vector2Int, GameObject> _marcadores = new Dictionary<Vector2Int, GameObject>();
    Camera _cam;
    CamaraSigue _sigue;

    void Awake()
    {
        Actual = this;
        _fuerte = GetComponent<Fuerte>();
    }

    void OnEnable()
    {
        PiezaFuerte.AlGolpear += Golpe;
        ControlJugador.SinNadaQueUsar = MirarSuelo;
    }

    void OnDisable()
    {
        PiezaFuerte.AlGolpear -= Golpe;
        if (ControlJugador.SinNadaQueUsar == MirarSuelo) ControlJugador.SinNadaQueUsar = null;
        if (Actual == this) Actual = null;
    }

    void Start()
    {
        _jugador = FindAnyObjectByType<ControlJugador>();
        _ui = PantallaSaqueo.Crear(this);
        if (Datos == null)
        {
            Error = "El saqueo necesita el servidor";
            _ui.Refrescar();
            return;
        }
        // Trampas y defensores no actúan en esta versión del saqueo.
        foreach (var t in FindObjectsByType<Trampa>()) t.enabled = false;
        foreach (var e in Enemigo.Todos.ToArray()) if (e) e.enabled = false;
        // El cofre central también se mira.
        var c = ReglasFuerte.Cofre(_fuerte.Mapa);
        var central = new GameObject("Mirar cofre central").AddComponent<Mirable>();
        central.transform.position = _fuerte.Mundo(c.x, c.y);
        central.x = c.x; central.z = c.y; central.alcance = 1.6f;
        PonerEstandarte();
        // Salir por la entrada termina el saqueo sin marcar nada.
        foreach (var p in FindObjectsByType<Puerta>())
            if (p.destino == "Pueblo") p.antesDeIr = () => { if (Estado == Fase.Explorar) EmpezarMarcar(); return false; };
        Estado = Fase.Explorar;
        Apuntar(null);
        _ui.Refrescar();
    }

    void Update()
    {
        if (Transicion.Ocupada) return;
        if (Estado == Fase.Explorar || Estado == Fase.Mirando)
        {
            _t += Time.deltaTime;
            if (_t >= _siguienteEntrada) Apuntar(null);
            if (_t >= Duracion && Estado == Fase.Explorar) EmpezarMarcar();
        }
        else if (Estado == Fase.Marcar && Input.GetMouseButtonDown(0))
        {
            int dedo = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(dedo)) return;
            TocarPantalla(Input.mousePosition);
        }
    }

    // ---------- explorar ----------

    /// Posición del jugador en casillas del fuerte (con decimales), como las espera la API.
    Vector2 PosicionLocal()
    {
        var l = _fuerte.origen.InverseTransformPoint(_jugador ? _jugador.transform.position : Vector3.zero);
        return new Vector2(l.x, l.z);
    }

    void Apuntar(MensajesApi.Casilla golpe)
    {
        var p = PosicionLocal();
        _entradas.Add(new MensajesApi.EntradaSaqueo { t = (float)Math.Round(_t, 2), x = p.x, z = p.y, golpea = golpe != null, golpe = golpe });
        _siguienteEntrada = _t + CadaEntrada;
    }

    void Golpe(PiezaFuerte p)
    {
        if (Estado != Fase.Explorar) return;
        Apuntar(new MensajesApi.Casilla { x = p.x, z = p.z });
    }

    /// Botón de interactuar sin cofre ni barril cerca: mira la casilla de delante.
    bool MirarSuelo(ControlJugador j)
    {
        if (!PuedeMirar) return false;
        var c = _fuerte.Casilla(j.transform.position + j.transform.forward * 0.9f);
        Mirar(c.x, c.y);
        return true;
    }

    /// Mirar una casilla: 1 s parado y la API dice qué se oye (08 §4.2).
    public void Mirar(int x, int z)
    {
        if (!PuedeMirar) return;
        StartCoroutine(Mirando(x, z));
    }

    IEnumerator Mirando(int x, int z)
    {
        Estado = Fase.Mirando;
        if (_jugador) _jugador.enabled = false;
        EntradaJuego.MoverPantalla = Vector2.zero;
        _ui.MostrarMirando(Datos.mirar_segundos);
        Apuntar(null);
        yield return new WaitForSeconds(Datos.mirar_segundos);
        bool listo = false;
        string respuesta = "";
        ApiCliente.Post("/saqueo/mirar", new MensajesApi.PeticionMirar { saqueo_id = Datos.saqueo_id, x = x, z = z, t = _t }, r =>
        {
            respuesta = r.Ok ? r.Leer<MensajesApi.RespuestaMirar>().respuesta : r.Mensaje;
            listo = true;
        });
        while (!listo) yield return null;
        UltimaMirada = respuesta;
        NumeroFlotante.Crear(_fuerte.Mundo(x, z) + Vector3.up * 1.4f, respuesta, new Color(0.95f, 0.85f, 0.5f), 0.9f);
        _ui.MostrarMirada(respuesta);
        if (_jugador) _jugador.enabled = true;
        if (Estado == Fase.Mirando) Estado = _t >= Duracion ? Estado : Fase.Explorar;
        if (_t >= Duracion) EmpezarMarcar();
    }

    // ---------- marcar ----------

    /// "Saquear" o fin del tiempo: se para el juego, la cámara sube y se marcan escondites (08 §4.3).
    public void EmpezarMarcar()
    {
        if (Estado != Fase.Explorar && Estado != Fase.Mirando) return;
        Apuntar(null);
        Estado = Fase.Marcar;
        if (_jugador) _jugador.enabled = false;
        EntradaJuego.MoverPantalla = Vector2.zero;
        foreach (var n in new[] { "Joystick", "BotonAtaque", "BotonInteractuar", "BotonCorrer" })
        {
            var g = GameObject.Find("Interfaz/" + n);
            if (g) g.SetActive(false);
        }
        _cam = Camera.main;
        _sigue = _cam ? _cam.GetComponent<CamaraSigue>() : null;
        if (_sigue) _sigue.enabled = false;
        if (_cam)
        {
            Vector3 centro = _fuerte.Mundo(_fuerte.Mapa.Ancho / 2, _fuerte.Mapa.Alto / 2) + new Vector3(-0.5f, 0f, -0.5f);
            _cam.transform.position = centro + new Vector3(0f, 17f, -9f);
            _cam.transform.LookAt(centro + Vector3.back * 1.6f);
            _cam.fieldOfView = 52f;
        }
        _ui.Refrescar();
    }

    public bool TocarPantalla(Vector2 punto)
    {
        if (!_cam) _cam = Camera.main;
        var rayo = _cam.ScreenPointToRay(punto);
        var plano = new Plane(Vector3.up, _fuerte.origen.position);
        if (!plano.Raycast(rayo, out float d)) return false;
        var c = _fuerte.Casilla(rayo.GetPoint(d));
        return AlternarMarca(c.x, c.y);
    }

    /// Pone o quita la marca de una casilla. Solo dentro del fuerte y hasta "acciones" marcas.
    public bool AlternarMarca(int x, int z)
    {
        if (Estado != Fase.Marcar) return false;
        var k = new Vector2Int(x, z);
        if (_marcadores.TryGetValue(k, out var g))
        {
            Destroy(g);
            _marcadores.Remove(k);
            _marcas.RemoveAll(m => m.x == x && m.z == z);
            _ui.Refrescar();
            return true;
        }
        char l = ReglasFuerte.Letra(_fuerte.Mapa, x, z);
        if (l != '.' && l != 'K') { Avisos.Mostrar("Eso está fuera del fuerte", 1.2f); return false; }
        if (_marcas.Count >= Acciones) { Avisos.Mostrar($"Solo tienes {Acciones} acciones: quita una marca para poner otra", 1.8f); return false; }
        _marcas.Add(new MensajesApi.Casilla { x = x, z = z });
        // Disco dorado en el suelo (se ve desde arriba) y el estandarte de la marca encima.
        var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m.name = $"Marca ({x},{z})";
        m.transform.position = _fuerte.Mundo(x, z) + new Vector3(0f, 0.03f, 0f);
        m.transform.localScale = new Vector3(0.85f, 0.03f, 0.85f);
        var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.8f, 0.2f, 0.85f) };
        m.GetComponent<Renderer>().sharedMaterial = mat;
        var prefab = Catalogo.Instancia ? Catalogo.Instancia.marcaSaqueo : null;
        if (prefab)
        {
            var b = Instantiate(prefab, _fuerte.Mundo(x, z), Quaternion.identity);
            b.SetActive(true);
            b.transform.SetParent(m.transform, true);
        }
        foreach (var col in m.GetComponentsInChildren<Collider>()) col.enabled = false;
        _marcadores[k] = m;
        Sonidos.Tocar("golpe_madera", null, 0.5f);
        _ui.Refrescar();
        return true;
    }

    // ---------- resolver ----------

    public void Confirmar()
    {
        if (Estado != Fase.Marcar) return;
        Estado = Fase.Resolviendo;
        _ui.Refrescar();
        var pet = new MensajesApi.PeticionResolver { saqueo_id = Datos.saqueo_id, marcas = new List<MensajesApi.Casilla>(_marcas), entradas = _entradas };
        ApiCliente.Post("/saqueo/resolver", pet, r =>
        {
            if (!r.Ok)
            {
                Error = r.Mensaje;
                Estado = Fase.Terminado;
                Avisos.Mostrar("No se pudo resolver el saqueo: " + r.Mensaje, 3f);
                _ui.MostrarResultado(null);
                return;
            }
            Resultado = r.Leer<MensajesApi.RespuestaResolver>();
            Sincronizacion.AplicarJugador(Resultado.jugador);
            StartCoroutine(Secuencia());
        });
    }

    /// Lo que salió en cada marca, una detrás de otra (08 §4.4), y la pantalla de resultado.
    IEnumerator Secuencia()
    {
        foreach (var m in Resultado.marcas)
        {
            string texto; Color color;
            switch (m.resultado)
            {
                case "botin": texto = "+" + MensajesApi.Texto(m.botin); color = new Color(1f, 0.85f, 0.3f); Sonidos.Tocar("recoger_moneda"); break;
                case "vacio": texto = "Vacío"; color = Color.white; Sonidos.Tocar("golpe_madera"); break;
                case "inalcanzable": texto = "Sin camino"; color = new Color(1f, 0.55f, 0.5f); break;
                default: texto = "Nada"; color = new Color(0.8f, 0.8f, 0.8f); break;
            }
            NumeroFlotante.Crear(_fuerte.Mundo(m.x, m.z) + Vector3.up * 1.2f, texto, color, 1.3f);
            yield return new WaitForSeconds(0.8f);
        }
        Estado = Fase.Terminado;
        Sonidos.Tocar(EncontroAlgo ? "recoger_moneda" : "morir");
        _ui.MostrarResultado(Resultado);
        AlTerminar?.Invoke(Resultado);
    }

    bool EncontroAlgo => Resultado != null && Resultado.marcas.Exists(m => m.resultado == "botin");

    /// Vuelta al pueblo junto al cofre del saqueo.
    public void Volver()
    {
        Datos = null;
        Viaje.llegarA = "asalto";
        Transicion.Ir("Pueblo");
    }

    // ---------- estandarte ----------

    /// El estandarte de la temporada del rival junto a la entrada (08 §1.3), teñido con el color de su tramo.
    void PonerEstandarte()
    {
        if (Rival == null || string.IsNullOrEmpty(Rival.estandarte) || !Catalogo.Instancia || !Catalogo.Instancia.estandarte) return;
        var l = ReglasFuerte.Llegada(_fuerte.Mapa);
        var g = Instantiate(Catalogo.Instancia.estandarte, _fuerte.Mundo(l.x, l.y) + new Vector3(0f, 0f, 1f), Quaternion.Euler(0f, 90f, 0f));
        g.SetActive(true);
        var color = ColorTramo(Rival.estandarte);
        foreach (var r in g.GetComponentsInChildren<Renderer>()) r.material.color = color;
    }

    public static Color ColorTramo(string tramo)
    {
        switch (tramo)
        {
            case "plata": return new Color(0.78f, 0.8f, 0.85f);
            case "oro": return new Color(0.95f, 0.76f, 0.25f);
            case "hierro_orco": return new Color(0.35f, 0.45f, 0.38f);
            default: return new Color(0.72f, 0.45f, 0.25f);
        }
    }

    public static string NombreTramo(string tramo)
    {
        switch (tramo)
        {
            case "plata": return "Plata";
            case "oro": return "Oro";
            case "hierro_orco": return "Hierro Orco";
            case "bronce": return "Bronce";
            default: return "";
        }
    }
}
