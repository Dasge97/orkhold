using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Piezas de interfaz hechas desde código (uGUI), con la misma escala para todas las pantallas:
/// referencia 1920x1080, mitad ancho y mitad alto. Aspecto del UI Pack RPG y las letras de Kenney (15-interfaz.md §2).
public static class Ui
{
    public static readonly Color Panel = new Color(0.1f, 0.11f, 0.15f, 0.97f);
    public static readonly Color Velo = new Color(0f, 0f, 0f, 0.55f);
    public static readonly Color Verde = new Color(0.22f, 0.5f, 0.3f, 1f);
    public static readonly Color Rojo = new Color(0.45f, 0.2f, 0.2f, 1f);
    public static readonly Color Gris = new Color(0.22f, 0.24f, 0.3f, 1f);
    public static readonly Color Dorado = new Color(0.95f, 0.75f, 0.25f, 1f);
    /// Texto claro sobre madera y texto oscuro sobre pergamino.
    public static readonly Color Crema = new Color(1f, 0.94f, 0.82f, 1f);
    public static readonly Color Tinta = new Color(0.24f, 0.16f, 0.1f, 1f);

    static Font _fuente, _titulo;
    /// Letra de los textos largos: la de Unity, con minúsculas (las de Kenney con tildes solo tienen mayúsculas).
    public static Font Fuente => _fuente ? _fuente : (_fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
    /// Letra de títulos, nombres, botones y números (Kenney Future).
    public static Font FuenteTitulo => _titulo ? _titulo : (_titulo = Resources.Load<Font>("Fuentes/Titulo") ?? Fuente);

    static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

    /// Sprite de Resources/Ui ("panel_brown") o de Resources/Iconos ("Iconos/moneda"). Null si no existe.
    public static Sprite Sprite(string nombre)
    {
        if (_sprites.TryGetValue(nombre, out var s)) return s;
        s = Resources.Load<Sprite>(nombre.Contains("/") ? nombre : "Ui/" + nombre);
        _sprites[nombre] = s;
        return s;
    }

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

    /// Recuadro con imagen estirable del pack ("panel_brown", "panelInset_beige"...).
    public static RectTransform Marco(Transform padre, string nombre, string sprite, Vector2 pos, Vector2 tam, Vector2? ancla = null, Color? tinte = null)
    {
        var rt = Caja(padre, nombre, tinte ?? Color.white, pos, tam, ancla);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f;   // bordes al doble: se ven a 1080 como en el pack a 540
        return rt;
    }

    /// Imagen suelta (icono, retrato) que conserva su forma.
    public static Image Icono(Transform padre, string sprite, Vector2 pos, Vector2 tam, Vector2? ancla = null)
    {
        var rt = Caja(padre, "Icono", Color.white, pos, tam, ancla);
        var img = rt.GetComponent<Image>();
        img.sprite = Sprite(sprite);
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (!img.sprite) img.color = new Color(1f, 1f, 1f, 0f);
        return img;
    }

    /// Velo oscuro a pantalla completa con un panel de madera centrado.
    public static RectTransform PanelCentrado(Transform padre, Vector2 tam, out GameObject velo)
    {
        var v = Caja(padre, "Velo", Velo, Vector2.zero, Vector2.zero);
        v.anchorMin = Vector2.zero; v.anchorMax = Vector2.one; v.offsetMin = v.offsetMax = Vector2.zero;
        velo = v.gameObject;
        return Marco(v, "Panel", "panel_brown", Vector2.zero, tam);
    }

    public static Text Texto(Transform padre, string texto, int tam, Vector2 pos, Vector2 caja,
                             TextAnchor alineado = TextAnchor.MiddleCenter, bool negrita = false)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = Fuente; t.fontSize = tam; t.color = Crema; t.alignment = alineado;
        t.fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal;
        t.raycastTarget = false; t.text = texto; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.1f;
        var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = caja;
        Sombra(t);
        return t;
    }

    /// Título en la letra de títulos, dorado y con contorno.
    public static Text Titulo(Transform padre, string texto, int tam, Vector2 pos, Vector2 caja, TextAnchor alineado = TextAnchor.MiddleCenter)
    {
        var t = Texto(padre, texto, tam, pos, caja, alineado);
        t.font = FuenteTitulo; t.color = Dorado;
        Contorno(t);
        return t;
    }

    public static void Sombra(Graphic g, float d = 2f)
    {
        var s = g.gameObject.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.6f); s.effectDistance = new Vector2(d, -d);
    }

    public static void Contorno(Graphic g, float d = 2.5f)
    {
        var o = g.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.95f); o.effectDistance = new Vector2(d, -d);
    }

    /// Botón de madera del pack, teñido con "color" (verde, rojo, gris...).
    public static Button Boton(Transform padre, string texto, Vector2 pos, Vector2 tam, Color color, UnityAction accion, int tamLetra = 40)
    {
        var rt = Marco(padre, "Boton " + texto, "buttonLong_beige", pos, tam, null, Tinte(color));
        var img = rt.GetComponent<Image>();
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState { pressedSprite = Sprite("buttonLong_beige_pressed"), disabledSprite = Sprite("buttonLong_grey") };
        b.onClick.AddListener(accion);
        var t = Texto(rt, texto, tamLetra, new Vector2(0f, 3f), tam - new Vector2(24f, 0f));
        t.name = "Etiqueta";
        t.font = FuenteTitulo;
        t.color = Color.white;
        Contorno(t, 2f);
        return b;
    }

    /// Los colores de siempre, más vivos: el sprite beige los oscurece al multiplicar.
    static Color Tinte(Color c)
    {
        if (c.a < 0.99f) return new Color(0.85f, 0.75f, 0.6f, 1f);
        Color.RGBToHSV(c, out float h, out float s, out float v);
        var r = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.1f), Mathf.Min(1f, v * 1.7f + 0.1f));
        r.a = 1f;
        return r;
    }

    /// Barra con fondo y relleno del pack ("barRed", "barGreen", "barYellow"). Devuelve el relleno: cambiar fillAmount.
    public static Image Barra(Transform padre, string color, Vector2 pos, Vector2 tam, Vector2? ancla = null)
    {
        var fondo = Caja(padre, "Barra", Color.white, pos, tam, ancla);
        fondo.GetComponent<Image>().sprite = Sprite("barBack_horizontalMid");
        fondo.GetComponent<Image>().raycastTarget = false;
        var r = Caja(fondo, "Relleno", Color.white, Vector2.zero, Vector2.zero);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(3f, 3f); r.offsetMax = new Vector2(-3f, -3f);
        var img = r.GetComponent<Image>();
        img.sprite = Sprite(color + "_horizontalMid");
        img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal;
        img.raycastTarget = false;
        return img;
    }

    /// Campo de texto (abre el teclado en el móvil).
    public static InputField Entrada(Transform padre, string ayuda, Vector2 pos, Vector2 tam, int maximo)
    {
        var rt = Marco(padre, "Entrada", "panelInset_beige", pos, tam);
        var campo = rt.gameObject.AddComponent<InputField>();
        var t = Texto(rt, "", 44, Vector2.zero, tam - new Vector2(40f, 0f), TextAnchor.MiddleLeft);
        t.color = Tinta; t.supportRichText = false;
        Object.Destroy(t.GetComponent<Shadow>());
        var marca = Texto(rt, ayuda, 44, Vector2.zero, tam - new Vector2(40f, 0f), TextAnchor.MiddleLeft);
        marca.color = new Color(Tinta.r, Tinta.g, Tinta.b, 0.45f); marca.fontStyle = FontStyle.Italic;
        Object.Destroy(marca.GetComponent<Shadow>());
        campo.textComponent = t;
        campo.placeholder = marca;
        campo.characterLimit = maximo;
        campo.targetGraphic = rt.GetComponent<Image>();
        return campo;
    }
}
