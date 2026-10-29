using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Interfaz del modo construir: botón "Construir" al andar por el fuerte; al construir, barra inferior
/// con las piezas (nombre, coste, usadas/límite), "Mejorar" y "Listo", y una línea de ayuda arriba.
public class PantallaConstruir : MonoBehaviour
{
    static readonly Color Fondo = new Color(0.08f, 0.09f, 0.12f, 0.85f);
    static readonly Color Elegido = new Color(0.95f, 0.7f, 0.2f, 0.95f);
    static readonly Color Verde = new Color(0.2f, 0.6f, 0.3f, 0.95f);

    EditorFuerte _editor;
    GameObject _botonConstruir, _barra, _botonListo;
    Text _ayuda;
    readonly Dictionary<string, (Image fondo, Text texto)> _botones = new Dictionary<string, (Image, Text)>();
    static Font _fuente;

    public static PantallaConstruir Crear(EditorFuerte editor)
    {
        if (!_fuente) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("PantallaConstruir");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var p = go.AddComponent<PantallaConstruir>();
        p._editor = editor;
        p.Montar();
        p.Refrescar();
        return p;
    }

    void Montar()
    {
        _botonConstruir = Boton(transform, "Construir", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 110f), Verde, _editor.Entrar, 46).gameObject;

        _barra = new GameObject("Barra");
        _barra.transform.SetParent(transform, false);
        var rt = _barra.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 170f); rt.anchoredPosition = Vector2.zero;
        _barra.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
        var fila = _barra.AddComponent<HorizontalLayoutGroup>();
        fila.padding = new RectOffset(16, 16, 14, 14); fila.spacing = 12f;
        fila.childForceExpandWidth = true; fila.childForceExpandHeight = true; fila.childControlWidth = true; fila.childControlHeight = true;

        var cat = CatalogoPiezas.Instancia;
        if (cat)
            foreach (var pieza in cat.piezas)
            {
                string id = pieza.id;
                var b = Boton(_barra.transform, "", Vector2.zero, Vector2.zero, Vector2.zero, Fondo, () => _editor.Seleccionar(id), 28);
                _botones[id] = (b.GetComponent<Image>(), b.GetComponentInChildren<Text>());
            }
        var bm = Boton(_barra.transform, "", Vector2.zero, Vector2.zero, Vector2.zero, Fondo, () => _editor.Seleccionar(EditorFuerte.Mejorar), 28);
        _botones[EditorFuerte.Mejorar] = (bm.GetComponent<Image>(), bm.GetComponentInChildren<Text>());

        _botonListo = Boton(transform, "Listo", new Vector2(1f, 1f), new Vector2(-310f, -80f), new Vector2(240f, 110f), Verde, _editor.Salir, 44).gameObject;

        var ayuda = new GameObject("Ayuda").AddComponent<Text>();
        ayuda.transform.SetParent(transform, false);
        ayuda.font = _fuente; ayuda.fontSize = 32; ayuda.color = Color.white; ayuda.alignment = TextAnchor.UpperCenter;
        ayuda.horizontalOverflow = HorizontalWrapMode.Overflow;
        ayuda.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        var art = ayuda.rectTransform;
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 1f); art.pivot = new Vector2(0.5f, 1f);
        art.anchoredPosition = new Vector2(0f, -150f); art.sizeDelta = new Vector2(1400f, 50f);
        _ayuda = ayuda;
    }

    public void Refrescar()
    {
        if (!_barra) return;
        bool c = _editor.Construyendo;
        _botonConstruir.SetActive(!c);
        _barra.SetActive(c);
        _botonListo.SetActive(c);
        _ayuda.gameObject.SetActive(c);
        if (!c) return;

        var cat = CatalogoPiezas.Instancia;
        var d = Partida.Actual.fuerte;
        foreach (var pieza in cat.piezas)
        {
            if (!_botones.TryGetValue(pieza.id, out var b)) continue;
            int usadas = d.Contar(q => q.tipo == pieza.id);
            string extra = string.IsNullOrEmpty(pieza.requiereUnidad) ? "" : $"\ntienes {Partida.Actual.unidades.FindAll(u => u.tipo == pieza.requiereUnidad).Count}";
            b.texto.text = $"<b>{pieza.nombre}</b>\n{pieza.TextoCoste()}{extra}\npuestas {usadas}";
            b.fondo.color = _editor.Seleccion == pieza.id ? Elegido : Fondo;
        }
        var m = _botones[EditorFuerte.Mejorar];
        m.texto.text = $"<b>Mejorar</b>\n+1 vida defensor\n{cat.huesosPorMejora} huesos";
        m.fondo.color = _editor.Seleccion == EditorFuerte.Mejorar ? Elegido : Fondo;

        int Cat(CatalogoPiezas.Categoria k) => d.Contar(q => cat.Buscar(q.tipo)?.categoria == k);
        _ayuda.text = $"Muros {Cat(CatalogoPiezas.Categoria.Muro)}/{cat.maxMuros} · Trampas {Cat(CatalogoPiezas.Categoria.Trampa)}/{cat.maxTrampas} · " +
                      $"Defensores {Cat(CatalogoPiezas.Categoria.Defensor)}/{cat.maxDefensores} — toca una casilla para poner o quitar";
    }

    static Button Boton(Transform padre, string texto, Vector2 ancla, Vector2 pos, Vector2 tam, Color color, UnityEngine.Events.UnityAction accion, int tamLetra)
    {
        var g = new GameObject("Boton " + texto);
        g.transform.SetParent(padre, false);
        var rt = g.AddComponent<RectTransform>();
        if (tam != Vector2.zero)
        {
            rt.anchorMin = rt.anchorMax = ancla; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
        }
        var img = g.AddComponent<Image>();
        img.color = color;
        var b = g.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(accion);
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(g.transform, false);
        t.font = _fuente; t.fontSize = tamLetra; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.supportRichText = true; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.text = texto;
        var trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(6f, 4f); trt.offsetMax = new Vector2(-6f, -4f);
        return b;
    }
}
