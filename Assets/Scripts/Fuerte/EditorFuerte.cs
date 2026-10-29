using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// Modo construir del fuerte propio (01-diseno.md §6.3): cámara desde arriba, barra inferior con
/// piezas y coste, tocar una casilla para poner (o quitar si ya hay algo), "Mejorar" para dar
/// vida a un defensor con hueso y "Listo" para guardar y volver a andar.
[RequireComponent(typeof(Fuerte))]
public class EditorFuerte : MonoBehaviour
{
    public const string Mejorar = "mejorar";

    public bool Construyendo { get; private set; }
    public string Seleccion { get; private set; } = "muro_madera";
    public string UltimoMotivo { get; private set; } = "";

    /// Al poner, quitar o mejorar algo.
    public event Action AlCambiar;

    Fuerte _fuerte;
    Camera _cam;
    CamaraSigue _sigue;
    Vector3 _camPos;
    Quaternion _camRot;
    float _fov;
    GameObject _rejilla;
    PantallaConstruir _ui;

    public Fuerte Fuerte => _fuerte;

    void Awake() { _fuerte = GetComponent<Fuerte>(); }

    void Start() { _ui = PantallaConstruir.Crear(this); }

    public void Entrar()
    {
        if (Construyendo) return;
        Construyendo = true;
        // El jugador se aparta a la entrada (ahí no se construye). Antes de mover la cámara:
        // colocar al jugador recoloca la cámara que le sigue.
        var j = FindAnyObjectByType<ControlJugador>();
        var l = ReglasFuerte.Llegada(_fuerte.Mapa);
        if (j && l.x >= 0) GestorEscena.Colocar(j.transform, _fuerte.Mundo(l.x, l.y), 90f);
        _cam = Camera.main;
        _sigue = _cam ? _cam.GetComponent<CamaraSigue>() : null;
        if (_sigue) _sigue.enabled = false;
        if (_cam)
        {
            _camPos = _cam.transform.position; _camRot = _cam.transform.rotation; _fov = _cam.fieldOfView;
            Vector3 centro = _fuerte.Mundo(_fuerte.Mapa.Ancho / 2, _fuerte.Mapa.Alto / 2) + new Vector3(-0.5f, 0f, -0.5f);
            // Encuadre con sitio abajo para la barra de piezas y arriba para la ayuda.
            _cam.transform.position = centro + new Vector3(0f, 17f, -9f);
            _cam.transform.LookAt(centro + Vector3.back * 1.6f);
            _cam.fieldOfView = 52f;
        }
        MostrarControles(false);
        if (!_rejilla) _rejilla = CrearRejilla();
        _rejilla.SetActive(true);
        _ui.Refrescar();
    }

    /// "Listo": guarda y vuelve a andar.
    public void Salir()
    {
        if (!Construyendo) return;
        Construyendo = false;
        if (_cam)
        {
            _cam.transform.SetPositionAndRotation(_camPos, _camRot);
            _cam.fieldOfView = _fov;
        }
        if (_sigue) { _sigue.enabled = true; _sigue.Saltar(); }
        MostrarControles(true);
        if (_rejilla) _rejilla.SetActive(false);
        Guardado.Guardar();
        AlGuardar?.Invoke(Partida.Actual.fuerte);
        Avisos.Mostrar("Fuerte guardado", 1.5f);
        _ui.Refrescar();
    }

    /// Para la fase 6: subir el diseño a la API al pulsar "Listo".
    public static event Action<DisenoFuerte> AlGuardar;

    public void Seleccionar(string id)
    {
        Seleccion = id;
        _ui.Refrescar();
    }

    void Update()
    {
        if (!Construyendo || Transicion.Ocupada) return;
        if (!Input.GetMouseButtonDown(0)) return;
        int dedo = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(dedo)) return;
        TocarPantalla(Input.mousePosition);
    }

    /// Un toque en la pantalla: casilla bajo el dedo.
    public bool TocarPantalla(Vector2 punto)
    {
        if (!_cam) _cam = Camera.main;
        var rayo = _cam.ScreenPointToRay(punto);
        var plano = new Plane(Vector3.up, _fuerte.origen.position);
        if (!plano.Raycast(rayo, out float d)) return false;
        var c = _fuerte.Casilla(rayo.GetPoint(d));
        return TocarCasilla(c.x, c.y);
    }

    /// Poner la pieza elegida, quitar lo que haya o mejorar un defensor.
    public bool TocarCasilla(int x, int z)
    {
        var partida = Partida.Actual;
        var d = partida.fuerte;
        var cat = CatalogoPiezas.Instancia;
        bool ok;
        string motivo = "";
        string que;
        if (Seleccion == Mejorar) { ok = ReglasFuerte.Mejorar(d, cat, partida, x, z, out motivo); que = "mejorado"; }
        else if (d.En(x, z) != null) { ok = ReglasFuerte.Quitar(d, cat, partida, x, z); que = "quitado"; }
        else { ok = ReglasFuerte.Poner(d, cat, Seleccion, _fuerte.Mapa, partida, x, z, out motivo); que = "puesto"; }
        UltimoMotivo = motivo;
        if (!ok)
        {
            if (!string.IsNullOrEmpty(motivo)) Avisos.Mostrar(motivo, 1.5f);
            return false;
        }
        Sonidos.Tocar(que == "quitado" ? "recoger" : "golpe_madera", null, 0.7f);
        Partida.Cambiado();
        _fuerte.Reconstruir();
        _ui.Refrescar();
        AlCambiar?.Invoke();
        return true;
    }

    void MostrarControles(bool si)
    {
        foreach (var n in new[] { "Joystick", "BotonAtaque", "BotonInteractuar", "BotonCorrer" })
        {
            var g = GameObject.Find("Interfaz/" + n);
            if (g) g.SetActive(si);
        }
        EntradaJuego.MoverPantalla = Vector2.zero;
    }

    /// Cuadros claros en las casillas donde se puede construir.
    GameObject CrearRejilla()
    {
        var mapa = _fuerte.Mapa;
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        const float m = 0.44f;
        for (int x = 0; x < mapa.Ancho; x++)
            for (int z = 0; z < mapa.Alto; z++)
            {
                if (!ReglasFuerte.CasillaConstruible(mapa, x, z)) continue;
                int i = verts.Count;
                verts.Add(new Vector3(x - m, 0.02f, z - m)); verts.Add(new Vector3(x - m, 0.02f, z + m));
                verts.Add(new Vector3(x + m, 0.02f, z + m)); verts.Add(new Vector3(x + m, 0.02f, z - m));
                tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
        var malla = new Mesh { name = "Rejilla" };
        malla.SetVertices(verts); malla.SetTriangles(tris, 0); malla.RecalculateNormals();
        var g = new GameObject("Rejilla de construcción");
        g.transform.SetParent(_fuerte.origen, false);
        g.AddComponent<MeshFilter>().sharedMesh = malla;
        g.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 1f, 1f, 0.18f) };
        return g;
    }
}
