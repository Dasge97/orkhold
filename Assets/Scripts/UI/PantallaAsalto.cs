using UnityEngine;
using UnityEngine.UI;

/// Interfaz del asalto (01-diseno.md §6.4): cuenta atrás de 90 s arriba en el centro, barra
/// "distancia al cofre" debajo y, al terminar, la pantalla de resultado con las recompensas.
public class PantallaAsalto : MonoBehaviour
{
    Asalto _asalto;
    Text _reloj;
    RectTransform _relleno;
    GameObject _resultado;
    Text _titulo, _detalle;
    static Font _fuente;

    public static PantallaAsalto Crear(Asalto asalto)
    {
        if (!_fuente) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("PantallaAsalto");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var p = go.AddComponent<PantallaAsalto>();
        p._asalto = asalto;
        p.Montar();
        Asalto.AlTerminar += p.MostrarResultado;
        return p;
    }

    void OnDestroy() { Asalto.AlTerminar -= MostrarResultado; }

    void Montar()
    {
        // A la derecha del centro: a la izquierda está el HUD con vida y recursos.
        _reloj = Texto(transform, "1:30", 64, new Vector2(0.66f, 1f), new Vector2(0f, -55f), new Vector2(400f, 80f));

        var fondo = Caja("BarraCofre", transform, new Color(0f, 0f, 0f, 0.55f));
        fondo.anchorMin = fondo.anchorMax = new Vector2(0.66f, 1f); fondo.pivot = new Vector2(0.5f, 1f);
        fondo.anchoredPosition = new Vector2(0f, -110f); fondo.sizeDelta = new Vector2(560f, 30f);
        _relleno = Caja("Relleno", fondo, new Color(0.95f, 0.75f, 0.2f));
        _relleno.anchorMin = Vector2.zero; _relleno.anchorMax = new Vector2(0f, 1f);
        _relleno.offsetMin = new Vector2(3f, 3f); _relleno.offsetMax = new Vector2(-3f, -3f);
        Texto(fondo, "distancia al cofre", 22, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 30f));

        // Resultado.
        var velo = Caja("Resultado", transform, new Color(0f, 0f, 0f, 0.6f));
        velo.anchorMin = Vector2.zero; velo.anchorMax = Vector2.one; velo.offsetMin = velo.offsetMax = Vector2.zero;
        var panel = Caja("Panel", velo, new Color(0.1f, 0.11f, 0.15f, 0.97f));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f); panel.sizeDelta = new Vector2(900f, 640f);
        _titulo = Texto(panel, "", 80, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(860f, 110f));
        _detalle = Texto(panel, "", 40, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(840f, 300f));
        var b = Caja("Volver", panel, new Color(0.22f, 0.5f, 0.3f, 1f));
        b.anchorMin = b.anchorMax = new Vector2(0.5f, 0f); b.anchoredPosition = new Vector2(0f, 90f); b.sizeDelta = new Vector2(480f, 100f);
        b.gameObject.AddComponent<Button>().onClick.AddListener(() => _asalto.Volver());
        Texto(b, "Volver al pueblo", 42, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 100f));
        _resultado = velo.gameObject;
        _resultado.SetActive(false);
    }

    void Update()
    {
        if (!_asalto) return;
        int s = Mathf.CeilToInt(_asalto.Restante);
        _reloj.text = $"{s / 60}:{s % 60:00}";
        _reloj.color = s <= 15 ? new Color(1f, 0.4f, 0.35f) : Color.white;
        float k = _asalto.DistanciaInicial > 0f ? 1f - Mathf.Clamp01(_asalto.Distancia / _asalto.DistanciaInicial) : 0f;
        _relleno.anchorMax = new Vector2(k, 1f);
    }

    public bool ResultadoVisible => _resultado && _resultado.activeSelf;

    void MostrarResultado(Asalto.Resultado r)
    {
        _titulo.text = r.ganado ? "¡Victoria!" : "Derrota";
        _titulo.color = r.ganado ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.45f, 0.4f);
        string motivo = r.motivo switch
        {
            Asalto.Fin.Ganado => "Has tocado el cofre del tesoro",
            Asalto.Fin.PerdidoMuerte => "Has muerto",
            Asalto.Fin.PerdidoTiempo => "Se acabó el tiempo",
            Asalto.Fin.Abandonado => "Te has retirado",
            _ => "",
        };
        var premios = new System.Collections.Generic.List<string>();
        foreach (var rec in DatosInventario.Todos)
            if (r.recompensa[rec] > 0) premios.Add($"+{r.recompensa[rec]} {Nombres.Plural(rec, r.recompensa[rec])}");
        int dif = r.puntosDespues - r.puntosAntes;
        _detalle.text = $"{motivo}\n{r.segundos} s · {r.murosRotos} muros rotos\n{string.Join("  ", premios)}\n" +
                        $"Puntuación {r.puntosDespues} ({(dif >= 0 ? "+" : "")}{dif})";
        _resultado.SetActive(true);
    }

    static RectTransform Caja(string nombre, Transform padre, Color color)
    {
        var g = new GameObject(nombre).AddComponent<Image>();
        g.transform.SetParent(padre, false);
        g.color = color;
        return g.rectTransform;
    }

    static Text Texto(Transform padre, string texto, int tam, Vector2 ancla, Vector2 pos, Vector2 caja)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = _fuente; t.fontSize = tam; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; t.text = texto; t.fontStyle = FontStyle.Bold;
        t.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = ancla; rt.anchoredPosition = pos; rt.sizeDelta = caja;
        return t;
    }
}
