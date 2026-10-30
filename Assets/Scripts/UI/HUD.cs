using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// HUD (15-interfaz.md §3): arriba a la izquierda tu retrato, tu nombre, la vida y lo guardado en los escondites;
/// arriba a la derecha las monedas y los recursos con su icono; arriba en el centro el nombre del sitio al entrar;
/// abajo, los avisos de botín ("+3 Madera"). Se monta solo desde código sobre su RectTransform.
public class HUD : MonoBehaviour
{
    [Tooltip("Ya no se usa: el HUD se monta solo. Se deja para no romper el prefab.")]
    public Text texto;
    public Vida vida;
    public Inventario inventario;

    static readonly Recurso[] Orden = { Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso, Recurso.Pocion };
    static readonly Recurso[] TodosRecursos = { Recurso.Moneda, Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso, Recurso.Pocion };

    Image _retrato, _barraVida, _barraEscondites;
    Text _nombre, _textoVida, _textoEscondites, _monedas;
    GameObject _filaEscondites;
    readonly Dictionary<Recurso, Text> _contadores = new Dictionary<Recurso, Text>();
    readonly Dictionary<Recurso, float> _salto = new Dictionary<Recurso, float>();
    readonly int[] _antes = new int[6];
    float _desde;
    RectTransform _cartelLugar, _avisos;
    float _cartelT = -1f;
    string _retratoPuesto = "";

    /// Texto del HUD en una línea (para los tests y el registro): vida y recursos.
    public string Resumen => (vida ? $"Vida {vida.actual}/{vida.maxima} · " : "") + $"Monedas {Partida.Inventario.monedas}";
    /// Nombre del sitio en el cartel de arriba (para los tests).
    public string Lugar { get; private set; } = "";

    void Start()
    {
        if (texto) texto.enabled = false;
        Montar();
        Partida.AlCambiar += AlCambiarPartida;
        Enganchar();
        Instantanea();
        _desde = Time.unscaledTime + 1.5f;
        Refrescar();
        Lugar = Transicion.NombreLugar(SceneManager.GetActiveScene().name);
        if (Lugar != "") { _cartelLugar.GetComponentInChildren<Text>().text = Lugar; _cartelT = 0f; }
    }

    /// En el pueblo online el jugador aparece después: se engancha cuando haya uno.
    void Update()
    {
        if (!vida) Enganchar();
        Animar();
    }

    void Enganchar()
    {
        var j = ControlJugador.Local;
        if (!j) return;
        vida = j.GetComponent<Vida>();
        inventario = j.GetComponent<Inventario>();
        vida.AlCambiar += Refrescar; vida.AlMorir += Refrescar;
        // Cada golpe que recibes sacude un poco la cámara.
        vida.AlRecibirDano += (n, desde) => CamaraSigue.Sacudir(0.07f + 0.04f * n);
        // Tu nombre también sobre tu cabeza.
        if (!string.IsNullOrEmpty(Partida.Actual.nombre)) Rotulo.Poner(j.gameObject, Rotulo.Tipo.Jugador, Partida.Actual.nombre);
        Refrescar();
    }

    void OnDestroy() { Partida.AlCambiar -= AlCambiarPartida; }

    // ---------- montaje ----------

    void Montar()
    {
        var raiz = (RectTransform)transform;
        raiz.anchorMin = Vector2.zero; raiz.anchorMax = Vector2.one; raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        raiz.pivot = new Vector2(0.5f, 0.5f);

        // Arriba a la izquierda: retrato, nombre, vida y escondites.
        var yo = Ui.Marco(raiz, "Personaje", "panel_brown", new Vector2(24f, -20f), new Vector2(580f, 180f), new Vector2(0f, 1f));
        yo.pivot = new Vector2(0f, 1f);
        var marco = Ui.Marco(yo, "Marco", "panelInset_beige", new Vector2(92f, 0f), new Vector2(144f, 144f), new Vector2(0f, 0.5f));
        _retrato = Ui.Icono(marco, "", Vector2.zero, new Vector2(132f, 132f));
        _nombre = Ui.Titulo(yo, "", 34, new Vector2(180f, -40f), new Vector2(380f, 44f), TextAnchor.MiddleLeft);
        Arriba(_nombre.rectTransform);
        _nombre.color = Ui.Crema;
        _barraVida = Ui.Barra(yo, "barRed", new Vector2(180f, -88f), new Vector2(370f, 38f), new Vector2(0f, 1f));
        Arriba((RectTransform)_barraVida.transform.parent);
        _textoVida = Ui.Texto(_barraVida.transform.parent, "", 26, Vector2.zero, new Vector2(360f, 38f));
        _textoVida.font = Ui.FuenteTitulo; Ui.Contorno(_textoVida, 1.5f);
        _barraEscondites = Ui.Barra(yo, "barGreen", new Vector2(180f, -138f), new Vector2(370f, 26f), new Vector2(0f, 1f));
        _filaEscondites = _barraEscondites.transform.parent.gameObject;
        Arriba((RectTransform)_filaEscondites.transform);
        _textoEscondites = Ui.Texto(_filaEscondites.transform, "", 20, Vector2.zero, new Vector2(360f, 26f));
        Ui.Contorno(_textoEscondites, 1.5f);

        // Arriba a la derecha (a la izquierda del botón de menú): monedas grandes y recursos.
        var bolsa = Ui.Marco(raiz, "Recursos", "panel_brown", new Vector2(-170f, -20f), new Vector2(560f, 180f), new Vector2(1f, 1f));
        bolsa.pivot = new Vector2(1f, 1f);
        var moneda = Ui.Icono(bolsa, "Iconos/moneda", new Vector2(-470f, -52f), new Vector2(76f, 76f), new Vector2(1f, 1f));
        _monedas = Ui.Titulo(bolsa, "0", 52, new Vector2(-250f, -52f), new Vector2(330f, 64f), TextAnchor.MiddleLeft);
        Derecha(_monedas.rectTransform);
        _monedas.rectTransform.anchoredPosition = new Vector2(-420f, -52f);
        _contadores[Recurso.Moneda] = _monedas;
        for (int i = 0; i < Orden.Length; i++)
        {
            float x = -520f + i * 104f;
            Ui.Icono(bolsa, "Iconos/" + NombreIcono(Orden[i]), new Vector2(x + 26f, -128f), new Vector2(50f, 50f), new Vector2(1f, 1f));
            var t = Ui.Texto(bolsa, "0", 30, new Vector2(x + 54f, -128f), new Vector2(60f, 40f), TextAnchor.MiddleLeft);
            Derecha(t.rectTransform);
            t.rectTransform.anchoredPosition = new Vector2(x + 54f, -128f);
            t.font = Ui.FuenteTitulo; Ui.Contorno(t, 1.5f);
            _contadores[Orden[i]] = t;
        }

        // Arriba en el centro: el nombre del sitio al llegar.
        _cartelLugar = Ui.Marco(raiz, "Cartel lugar", "panel_brown", new Vector2(0f, 200f), new Vector2(700f, 110f), new Vector2(0.5f, 1f));
        _cartelLugar.pivot = new Vector2(0.5f, 1f);
        Ui.Titulo(_cartelLugar, "", 50, new Vector2(0f, 2f), new Vector2(680f, 90f));

        // Abajo en el centro: avisos de botín, apilados hacia arriba.
        var av = new GameObject("Avisos de botin", typeof(RectTransform));
        av.transform.SetParent(raiz, false);
        _avisos = (RectTransform)av.transform;
        _avisos.anchorMin = _avisos.anchorMax = new Vector2(0.5f, 0f);
        _avisos.anchoredPosition = new Vector2(0f, 150f); _avisos.sizeDelta = new Vector2(400f, 400f);
        _avisos.pivot = new Vector2(0.5f, 0f);
    }

    static void Arriba(RectTransform rt) { rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 0.5f); }
    static void Derecha(RectTransform rt) { rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0f, 0.5f); }

    public static string NombreIcono(Recurso r) =>
        r == Recurso.Moneda ? "moneda" : r == Recurso.Madera ? "madera" : r == Recurso.Piedra ? "piedra"
        : r == Recurso.Hierro ? "hierro" : r == Recurso.Hueso ? "hueso" : "pocion";

    public static string NombreRecurso(Recurso r, int n) =>
        r == Recurso.Moneda ? (n == 1 ? "moneda" : "monedas") : r == Recurso.Madera ? "madera" : r == Recurso.Piedra ? "piedra"
        : r == Recurso.Hierro ? "hierro" : r == Recurso.Hueso ? (n == 1 ? "hueso" : "huesos") : (n == 1 ? "poción" : "pociones");

    // ---------- datos ----------

    void AlCambiarPartida()
    {
        var inv = Partida.Inventario;
        // Botín: lo que sube de uno en uno o de poco en poco. Si sube casi todo a la vez es que llegó la partida
        // del servidor: no se avisa.
        int suben = 0;
        for (int i = 0; i < TodosRecursos.Length; i++) if (inv[TodosRecursos[i]] > _antes[i]) suben++;
        if (Time.unscaledTime > _desde && suben <= 3)
            for (int i = 0; i < TodosRecursos.Length; i++)
            {
                int d = inv[TodosRecursos[i]] - _antes[i];
                if (d > 0) { Botin(TodosRecursos[i], d); _salto[TodosRecursos[i]] = Time.unscaledTime; }
            }
        Instantanea();
        Refrescar();
    }

    void Instantanea()
    {
        for (int i = 0; i < TodosRecursos.Length; i++) _antes[i] = Partida.Inventario[TodosRecursos[i]];
    }

    void Refrescar()
    {
        if (!_nombre) return;
        var p = Partida.Actual;
        _nombre.text = string.IsNullOrEmpty(p.nombre) ? "Forastero" : p.nombre;
        string modelo = !string.IsNullOrEmpty(p.modelo) ? p.modelo : "character-male-a";
        var pj = vida ? vida.GetComponent<PersonajeJugador>() : null;
        if (pj && !string.IsNullOrEmpty(pj.Actual)) modelo = pj.Actual;
        if (modelo != _retratoPuesto)
        {
            _retratoPuesto = modelo;
            _retrato.sprite = Ui.Sprite("Iconos/retrato-" + modelo);
            _retrato.color = _retrato.sprite ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
        if (vida)
        {
            _barraVida.fillAmount = vida.actual / (float)Mathf.Max(1, vida.maxima);
            _textoVida.text = vida.Muerto ? "Has muerto" : $"{vida.actual} / {vida.maxima}";
        }
        // Escondites: verde; ámbar al 80 %; rojo lleno.
        bool hay = p.capacidad > 0;
        _filaEscondites.SetActive(hay);
        if (hay)
        {
            float f = Mathf.Clamp01(p.guardado / (float)p.capacidad);
            _barraEscondites.fillAmount = f;
            _barraEscondites.sprite = Ui.Sprite(f >= 1f ? "barRed_horizontalMid" : f >= 0.8f ? "barYellow_horizontalMid" : "barGreen_horizontalMid");
            _textoEscondites.text = f >= 1f ? "Escondites llenos" : $"Escondites {p.guardado} / {p.capacidad}";
        }
        var inv = Partida.Inventario;
        foreach (var kv in _contadores) kv.Value.text = inv[kv.Key].ToString();
    }

    // ---------- animación ----------

    void Animar()
    {
        float ahora = Time.unscaledTime;
        foreach (var kv in _contadores)
        {
            float t = _salto.TryGetValue(kv.Key, out var s) ? ahora - s : 9f;
            float e = t < 0.3f ? 1f + 0.35f * Mathf.Sin(t / 0.3f * Mathf.PI) : 1f;
            kv.Value.rectTransform.localScale = Vector3.one * e;
        }
        // Cartel del sitio: baja en 0,4 s, se queda 2,2 s y sube.
        if (_cartelT >= 0f)
        {
            _cartelT += Time.unscaledDeltaTime;
            float y = _cartelT < 0.4f ? Mathf.Lerp(200f, -215f, Suave(_cartelT / 0.4f))
                    : _cartelT < 2.6f ? -215f : Mathf.Lerp(-215f, 200f, Suave((_cartelT - 2.6f) / 0.4f));
            _cartelLugar.anchoredPosition = new Vector2(0f, y);
            if (_cartelT > 3.1f) _cartelT = -1f;
        }
    }

    static float Suave(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    /// "+3 madera" dorado con su icono; sube, se queda y se desvanece. Si ya hay uno del mismo recurso, se suma.
    void Botin(Recurso r, int n)
    {
        foreach (Transform hijo in _avisos)
        {
            var viejo = hijo.GetComponent<AvisoBotin>();
            if (viejo && viejo.recurso == r && viejo.Vivo) { viejo.Sumar(n); return; }
        }
        AvisoBotin.Crear(_avisos, r, n);
    }
}
