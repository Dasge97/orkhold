using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// Interfaz del saqueo (08-formato-y-pvp.md §4): reloj y botón "Saquear" al explorar, lo que se oye al mirar,
/// marcas usadas y "Confirmar" al marcar, y la pantalla de resultado con el botín y los puntos.
public class PantallaSaqueo : MonoBehaviour
{
    Saqueo _saqueo;
    Text _reloj, _ayuda, _mirada;
    GameObject _saquear, _confirmar, _resultado, _bomba;
    Text _pico;
    Text _titulo, _detalle;
    float _miradaHasta;

    public bool ResultadoVisible => _resultado && _resultado.activeSelf;
    public string TextoResultado => _detalle ? _titulo.text + "\n" + _detalle.text : "";

    public static PantallaSaqueo Crear(Saqueo s)
    {
        var go = Ui.Lienzo("PantallaSaqueo", 600);
        var p = go.AddComponent<PantallaSaqueo>();
        p._saqueo = s;
        p.Montar();
        return p;
    }

    void Montar()
    {
        // A la derecha del centro: a la izquierda está el HUD con vida y recursos.
        _reloj = Ui.Texto(transform, "", 64, new Vector2(0f, -55f), new Vector2(400f, 80f), negrita: true);
        Anclar(_reloj.rectTransform, new Vector2(0.62f, 1f));
        _reloj.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        _saquear = Ui.Boton(transform, "Saquear", new Vector2(0f, -150f), new Vector2(300f, 90f), Ui.Rojo, () => _saqueo.EmpezarMarcar(), 42).gameObject;
        Anclar((RectTransform)_saquear.transform, new Vector2(0.62f, 1f));
        _bomba = Ui.Boton(transform, "Bomba", new Vector2(-180f, 230f), new Vector2(240f, 90f), Ui.Rojo, () => _saqueo.LanzarBomba(), 36).gameObject;
        Anclar((RectTransform)_bomba.transform, new Vector2(1f, 0f));
        _pico = Ui.Texto(transform, "", 32, new Vector2(0f, -205f), new Vector2(500f, 50f));
        Anclar(_pico.rectTransform, new Vector2(0.62f, 1f));
        _ayuda = Ui.Texto(transform, "", 36, new Vector2(0f, -150f), new Vector2(1500f, 60f));
        Anclar(_ayuda.rectTransform, new Vector2(0.5f, 1f));
        _ayuda.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);
        _confirmar = Ui.Boton(transform, "Confirmar", new Vector2(-180f, 80f), new Vector2(320f, 110f), Ui.Verde, () => _saqueo.Confirmar(), 46).gameObject;
        Anclar((RectTransform)_confirmar.transform, new Vector2(1f, 0f));
        _mirada = Ui.Texto(transform, "", 48, new Vector2(0f, 230f), new Vector2(1200f, 80f), negrita: true);
        Anclar(_mirada.rectTransform, new Vector2(0.5f, 0f));
        _mirada.color = new Color(1f, 0.88f, 0.55f);
        _mirada.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);

        var panel = Ui.PanelCentrado(transform, new Vector2(1000f, 760f), out _resultado);
        _titulo = Ui.Texto(panel, "", 72, new Vector2(0f, 290f), new Vector2(940f, 100f), negrita: true);
        _detalle = Ui.Texto(panel, "", 36, new Vector2(0f, 20f), new Vector2(920f, 420f));
        Ui.Boton(panel, "Volver al pueblo", new Vector2(0f, -300f), new Vector2(480f, 100f), Ui.Verde, () => _saqueo.Volver(), 42);
        _resultado.SetActive(false);
        Refrescar();
    }

    static void Anclar(RectTransform rt, Vector2 ancla) { rt.anchorMin = rt.anchorMax = ancla; }

    public void Refrescar()
    {
        var e = _saqueo.Estado;
        bool explorar = e == Saqueo.Fase.Explorar || e == Saqueo.Fase.Mirando;
        _reloj.gameObject.SetActive(explorar);
        _saquear.SetActive(e == Saqueo.Fase.Explorar);
        _bomba.SetActive(e == Saqueo.Fase.Explorar && _saqueo.BombasQuedan > 0);
        if (_bomba.activeSelf) _bomba.GetComponentInChildren<Text>().text = $"Bomba ({_saqueo.BombasQuedan})";
        _confirmar.SetActive(e == Saqueo.Fase.Marcar);
        _ayuda.gameObject.SetActive(e == Saqueo.Fase.Marcar || e == Saqueo.Fase.Resolviendo || e == Saqueo.Fase.Sin);
        if (e == Saqueo.Fase.Marcar)
            _ayuda.text = $"Toca hasta {_saqueo.Acciones} casillas donde buscar · marcas {_saqueo.Marcas.Count}/{_saqueo.Acciones}";
        else if (e == Saqueo.Fase.Resolviendo) _ayuda.text = "Buscando...";
        else if (e == Saqueo.Fase.Sin && !string.IsNullOrEmpty(_saqueo.Error)) Panel("Saqueo", _saqueo.Error, Color.white);
    }

    public void MostrarMirando(float segundos) { _mirada.text = "Mirando..."; _miradaHasta = Time.time + segundos + 5f; }

    public void MostrarMirada(string texto) { _mirada.text = texto; _miradaHasta = Time.time + 2.5f; }

    void Update()
    {
        if (_reloj.gameObject.activeSelf)
        {
            int s = Mathf.CeilToInt(_saqueo.Restante);
            _reloj.text = $"{s / 60}:{s % 60:00}";
            _reloj.color = s <= 10 ? new Color(1f, 0.4f, 0.35f) : Color.white;
        }
        if (_mirada.text.Length > 0 && Time.time > _miradaHasta) _mirada.text = "";
        float pico = _saqueo.PicoQueda;
        _pico.text = pico > 0f && _reloj.gameObject.activeSelf ? $"Pico: {Mathf.CeilToInt(pico)} s" : "";
    }

    void Panel(string titulo, string texto, Color color)
    {
        foreach (var g in new[] { _reloj.gameObject, _saquear, _confirmar, _ayuda.gameObject }) g.SetActive(false);
        _resultado.SetActive(true);
        _titulo.text = titulo; _titulo.color = color;
        _detalle.text = texto;
    }

    public void MostrarResultado(MensajesApi.RespuestaResolver r)
    {
        if (r == null) { Panel("Saqueo", string.IsNullOrEmpty(_saqueo.Error) ? "No se pudo terminar el saqueo." : _saqueo.Error, Color.white); return; }
        Panel("", "", Color.white);
        int aciertos = r.marcas.FindAll(m => m.resultado == "botin").Count;
        _titulo.text = aciertos > 0 ? "¡Botín!" : "No encontraste nada";
        _titulo.color = aciertos > 0 ? Ui.Dorado : new Color(1f, 0.6f, 0.55f);
        var sb = new StringBuilder();
        foreach (var m in r.marcas)
        {
            string que = m.resultado == "botin" ? MensajesApi.Texto(m.botin) : m.resultado == "vacio" ? "vacío" :
                         m.resultado == "inalcanzable" ? "sin camino (un muro en medio)" : "nada";
            sb.AppendLine($"Casilla ({m.x},{m.z}): {que}");
        }
        if (r.marcas.Count == 0) sb.AppendLine("No marcaste ningún escondite.");
        sb.AppendLine();
        if (r.prueba)
            sb.AppendLine($"Fuerte de prueba: aquí te habrías llevado {MensajesApi.Texto(r.botin_prueba)}.\nNo hay botín real ni puntos.");
        else
        {
            sb.AppendLine($"Te llevas: {MensajesApi.Texto(r.botin)}");
            if (r.perdido > 0) sb.AppendLine($"No caben {r.perdido} en tus escondites: se pierden.");
            int dif = r.puntos_despues - r.puntos_antes;
            sb.AppendLine($"Puntos: {r.puntos_antes} → {r.puntos_despues} ({(dif >= 0 ? "+" : "")}{dif})");
            if (r.tasa_al_defensor > 0) sb.AppendLine($"La tasa ({r.tasa_al_defensor} monedas) se la queda el defensor.");
        }
        _detalle.text = sb.ToString().TrimEnd();
    }
}
