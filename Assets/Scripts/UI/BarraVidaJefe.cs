using UnityEngine;
using UnityEngine.UI;

/// Barra de vida del jefe arriba en el centro de la pantalla.
public class BarraVidaJefe : MonoBehaviour
{
    static BarraVidaJefe _instancia;
    RectTransform _relleno;
    Text _nombre;
    Vida _vida;

    public static bool Visible => _instancia && _instancia.gameObject.activeSelf;

    public static void Mostrar(Vida vida, string nombre)
    {
        if (!_instancia) Crear();
        _instancia._vida = vida;
        _instancia._nombre.text = nombre;
        _instancia.gameObject.SetActive(true);
        _instancia.Refrescar();
    }

    public static void Ocultar()
    {
        if (_instancia) _instancia.gameObject.SetActive(false);
    }

    static void Crear()
    {
        var go = new GameObject("BarraVidaJefe");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        _instancia = go.AddComponent<BarraVidaJefe>();

        var fondo = Caja("Fondo", go.transform, new Color(0f, 0f, 0f, 0.6f));
        fondo.anchorMin = fondo.anchorMax = new Vector2(0.5f, 1f);
        fondo.pivot = new Vector2(0.5f, 1f);
        fondo.anchoredPosition = new Vector2(0f, -40f);
        fondo.sizeDelta = new Vector2(760f, 34f);
        var rel = Caja("Relleno", fondo, new Color(0.85f, 0.15f, 0.12f));
        rel.anchorMin = Vector2.zero; rel.anchorMax = Vector2.one;
        rel.offsetMin = new Vector2(4f, 4f); rel.offsetMax = new Vector2(-4f, -4f);
        rel.pivot = new Vector2(0f, 0.5f);
        _instancia._relleno = rel;

        var t = new GameObject("Nombre").AddComponent<Text>();
        t.transform.SetParent(fondo, false);
        t.font = Ui.Fuente;
        t.fontSize = 30; t.fontStyle = FontStyle.Bold; t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        t.gameObject.AddComponent<Outline>();
        _instancia._nombre = t;
    }

    static RectTransform Caja(string nombre, Transform padre, Color color)
    {
        var g = new GameObject(nombre).AddComponent<Image>();
        g.transform.SetParent(padre, false);
        g.color = color;
        g.raycastTarget = false;
        return g.rectTransform;
    }

    void Update() => Refrescar();

    void Refrescar()
    {
        if (!_vida) { gameObject.SetActive(false); return; }
        float k = Mathf.Clamp01((float)_vida.actual / _vida.maxima);
        _relleno.anchorMax = new Vector2(k, 1f);
        if (_vida.Muerto) gameObject.SetActive(false);
    }
}
