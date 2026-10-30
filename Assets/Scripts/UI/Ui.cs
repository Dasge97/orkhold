using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Piezas de interfaz hechas desde código (uGUI), con la misma escala para todas las pantallas:
/// referencia 1920x1080, mitad ancho y mitad alto.
public static class Ui
{
    public static readonly Color Panel = new Color(0.1f, 0.11f, 0.15f, 0.97f);
    public static readonly Color Velo = new Color(0f, 0f, 0f, 0.55f);
    public static readonly Color Verde = new Color(0.22f, 0.5f, 0.3f, 1f);
    public static readonly Color Rojo = new Color(0.45f, 0.2f, 0.2f, 1f);
    public static readonly Color Gris = new Color(0.22f, 0.24f, 0.3f, 1f);
    public static readonly Color Dorado = new Color(0.95f, 0.75f, 0.25f, 1f);

    static Font _fuente;
    public static Font Fuente => _fuente ? _fuente : (_fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    public static GameObject Lienzo(string nombre, int orden)
    {
        var go = new GameObject(nombre);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    /// Rectángulo de color, anclado al centro de su padre salvo que se diga otra cosa.
    public static RectTransform Caja(Transform padre, string nombre, Color color, Vector2 pos, Vector2 tam, Vector2? ancla = null)
    {
        var g = new GameObject(nombre).AddComponent<Image>();
        g.transform.SetParent(padre, false);
        g.color = color;
        var rt = g.rectTransform;
        rt.anchorMin = rt.anchorMax = ancla ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        return rt;
    }

    /// Velo oscuro a pantalla completa con un panel centrado.
    public static RectTransform PanelCentrado(Transform padre, Vector2 tam, out GameObject velo)
    {
        var v = Caja(padre, "Velo", Velo, Vector2.zero, Vector2.zero);
        v.anchorMin = Vector2.zero; v.anchorMax = Vector2.one; v.offsetMin = v.offsetMax = Vector2.zero;
        velo = v.gameObject;
        return Caja(v, "Panel", Panel, Vector2.zero, tam);
    }

    public static Text Texto(Transform padre, string texto, int tam, Vector2 pos, Vector2 caja,
                             TextAnchor alineado = TextAnchor.MiddleCenter, bool negrita = false)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = Fuente; t.fontSize = tam; t.color = Color.white; t.alignment = alineado;
        t.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        t.raycastTarget = false; t.text = texto; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = caja;
        return t;
    }

    public static Button Boton(Transform padre, string texto, Vector2 pos, Vector2 tam, Color color, UnityAction accion, int tamLetra = 40)
    {
        var rt = Caja(padre, "Boton " + texto, color, pos, tam);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = rt.GetComponent<Image>();
        b.onClick.AddListener(accion);
        var t = Texto(rt, texto, tamLetra, Vector2.zero, tam);
        t.name = "Etiqueta";
        return b;
    }

    /// Campo de texto (abre el teclado en el móvil).
    public static InputField Entrada(Transform padre, string ayuda, Vector2 pos, Vector2 tam, int maximo)
    {
        var rt = Caja(padre, "Entrada", new Color(1f, 1f, 1f, 0.95f), pos, tam);
        var campo = rt.gameObject.AddComponent<InputField>();
        var t = Texto(rt, "", 44, Vector2.zero, tam - new Vector2(30f, 0f), TextAnchor.MiddleLeft);
        t.color = Color.black; t.supportRichText = false;
        var marca = Texto(rt, ayuda, 44, Vector2.zero, tam - new Vector2(30f, 0f), TextAnchor.MiddleLeft);
        marca.color = new Color(0f, 0f, 0f, 0.4f); marca.fontStyle = FontStyle.Italic;
        campo.textComponent = t;
        campo.placeholder = marca;
        campo.characterLimit = maximo;
        campo.targetGraphic = rt.GetComponent<Image>();
        return campo;
    }
}
