using System;
using UnityEngine;
using UnityEngine.UI;

/// En el cofre PvP del pueblo: "Buscar rival". Sin servidor, ofrece los tres fuertes de prueba
/// (fácil, medio, difícil); la fase 6 lo cambia por la búsqueda en la API (BuscarEnServidor).
[RequireComponent(typeof(Puerta))]
public class BuscarRival : MonoBehaviour
{
    public static readonly string[] NombresPrueba = { "fácil", "medio", "difícil" };

    /// Si no es null, se usa en vez del panel de fuertes de prueba (fase 6: la API elige el rival).
    public static Action BuscarEnServidor;

    /// Nombre del rival actual (para la pantalla de asalto).
    public static string Rival = "";

    GameObject _panel;
    static Font _fuente;

    void Start()
    {
        GetComponent<Puerta>().antesDeIr = () =>
        {
            if (BuscarEnServidor != null && !Configuracion.ModoLocal) BuscarEnServidor();
            else Abrir();
            return false;
        };
    }

    public bool Abierto => _panel && _panel.activeSelf;

    public void Abrir()
    {
        if (!_panel) Montar();
        _panel.SetActive(true);
    }

    public void Cerrar() { if (_panel) _panel.SetActive(false); }

    /// Asalta el fuerte de prueba "i" (0 fácil, 1 medio, 2 difícil).
    public void Elegir(int i)
    {
        var cat = Catalogo.Instancia;
        if (!cat || cat.fuertesPrueba == null || i >= cat.fuertesPrueba.Length) return;
        Cerrar();
        Ir(DisenoFuerte.DeJson(cat.fuertesPrueba[i].text), "Fuerte de prueba " + NombresPrueba[i]);
    }

    /// Empieza un asalto contra un diseño.
    public static void Ir(DisenoFuerte diseno, string rival)
    {
        Fuerte.DisenoAsalto = diseno;
        Rival = rival;
        Viaje.llegarA = "";
        Sonidos.Tocar("puerta");
        Transicion.Ir("Asalto");
    }

    void Montar()
    {
        if (!_fuente) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _panel = new GameObject("PanelRival");
        var canvas = _panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        var esc = _panel.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        _panel.AddComponent<GraphicRaycaster>();

        var velo = Caja(_panel.transform, new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.zero);
        velo.anchorMin = Vector2.zero; velo.anchorMax = Vector2.one;
        var fondo = Caja(velo, new Color(0.1f, 0.11f, 0.15f, 0.97f), Vector2.zero, new Vector2(1000f, 620f));
        Texto(fondo, "Asaltar un fuerte", 60, new Vector2(0f, 220f));
        Texto(fondo, "Sin servidor todavía: elige un fuerte de prueba.\n90 s para tocar el cofre del tesoro.", 34, new Vector2(0f, 110f));
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var b = Caja(fondo, new Color(0.22f, 0.45f, 0.3f, 1f), new Vector2(-300f + i * 300f, -40f), new Vector2(260f, 120f));
            b.gameObject.AddComponent<Button>().onClick.AddListener(() => Elegir(k));
            Texto(b, char.ToUpper(NombresPrueba[i][0]) + NombresPrueba[i].Substring(1), 44, Vector2.zero);
        }
        var cerrar = Caja(fondo, new Color(0.35f, 0.2f, 0.2f, 1f), new Vector2(0f, -210f), new Vector2(300f, 90f));
        cerrar.gameObject.AddComponent<Button>().onClick.AddListener(Cerrar);
        Texto(cerrar, "Cancelar", 40, Vector2.zero);
    }

    static RectTransform Caja(Transform padre, Color color, Vector2 pos, Vector2 tam)
    {
        var g = new GameObject("Caja").AddComponent<Image>();
        g.transform.SetParent(padre, false);
        g.color = color;
        var rt = g.rectTransform;
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        return rt;
    }

    static void Texto(Transform padre, string texto, int tam, Vector2 pos)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = _fuente; t.fontSize = tam; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; t.text = texto;
        var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(960f, 160f);
    }
}
