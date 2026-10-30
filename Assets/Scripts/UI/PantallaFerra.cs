using System.Collections.Generic;
using UnityEngine;

/// Ferra fabrica herramientas de saqueo (08-formato-y-pvp.md §5): farol y cuarta acción desde el principio;
/// pico, bomba, sabueso y señuelo cuando haya sus mazmorras. Se pagan de los escondites.
public class PantallaFerra : MonoBehaviour
{
    [System.Serializable] class Herramienta { public string id; public bool disponible; public MensajesApi.Recursos coste; }
    [System.Serializable] class Respuesta { public List<Herramienta> herramientas; }

    static readonly Dictionary<string, string> Que = new Dictionary<string, string>
    {
        ["farol"] = "Al mirar el suelo, dice si hay algo enterrado cerca",
        ["cuarta_accion"] = "Una marca más al saquear",
        ["pico"] = "15 s golpeando muros a triple daño (mazmorra 2)",
        ["bomba"] = "Rompe los muros de un 3x3 (mazmorra 2)",
        ["sabueso"] = "Ladra cerca del escondite más lleno (mazmorra 3)",
        ["senuelo"] = "Un defensor no pelea (mazmorra 3)",
    };

    public static PantallaFerra Abierta { get; private set; }
    public string Mensaje => _mensaje ? _mensaje.text : "";

    RectTransform _panel;
    GameObject _velo, _lista;
    UnityEngine.UI.Text _mensaje;
    List<Herramienta> _herramientas = new List<Herramienta>();

    public static PantallaFerra Abrir()
    {
        if (Abierta) Destroy(Abierta.gameObject);
        var go = Ui.Lienzo("PantallaFerra", 800);
        var p = go.AddComponent<PantallaFerra>();
        Abierta = p;
        p._panel = Ui.PanelCentrado(go.transform, new Vector2(1400f, 940f), out p._velo);
        Ui.Texto(p._panel, "Ferra, la herrera", 58, new Vector2(0f, 400f), new Vector2(1300f, 80f), negrita: true).color = Ui.Dorado;
        Ui.Texto(p._panel, "\"Hierro orco. Mal forjado, bien templado. Tráeme más.\"", 32, new Vector2(0f, 330f), new Vector2(1300f, 50f));
        p._mensaje = Ui.Texto(p._panel, "", 34, new Vector2(0f, -320f), new Vector2(1300f, 60f));
        Ui.Boton(p._panel, "Cerrar", new Vector2(0f, -400f), new Vector2(360f, 90f), Ui.Gris, p.Cerrar, 36);
        ApiCliente.Get("/herramientas", r =>
        {
            if (!p) return;
            if (!r.Ok) { p._mensaje.text = r.Mensaje; return; }
            p._herramientas = r.Leer<Respuesta>().herramientas;
            p.Pintar();
        });
        return p;
    }

    void OnDestroy() { if (Abierta == this) Abierta = null; }
    public void Cerrar() => Destroy(gameObject);

    void Pintar()
    {
        if (_lista) Destroy(_lista);
        _lista = new GameObject("Lista", typeof(RectTransform));
        _lista.transform.SetParent(_panel, false);
        for (int i = 0; i < _herramientas.Count; i++)
        {
            var h = _herramientas[i];
            float y = 240f - i * 90f;
            int tienes = Partida.Actual.unidades.FindAll(u => u.tipo == h.id).Count;
            string coste = MensajesApi.Texto(h.coste);
            Ui.Texto(_lista.transform, $"<b>{PantallaPrepararSaqueo.NombreHerramienta(h.id)}</b> · {coste} · tienes {tienes}\n<size=26>{Que[h.id]}</size>",
                32, new Vector2(-170f, y), new Vector2(1000f, 86f), TextAnchor.MiddleLeft);
            string id = h.id;
            if (h.disponible) Ui.Boton(_lista.transform, "Fabricar", new Vector2(510f, y), new Vector2(280f, 76f), Ui.Verde, () => Fabricar(id), 34);
            else Ui.Texto(_lista.transform, "Todavía no", 30, new Vector2(510f, y), new Vector2(280f, 76f)).color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    public void Fabricar(string id)
    {
        _mensaje.text = "Forjando...";
        ApiCliente.Post("/herramientas/fabricar", new MensajesApi.PeticionFabricar { id = id }, r =>
        {
            if (!this) return;
            if (!r.Ok) { _mensaje.text = r.Mensaje; return; }
            Sincronizacion.AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador);
            _mensaje.text = $"{PantallaPrepararSaqueo.NombreHerramienta(id)} lista.";
            Sonidos.Tocar("golpe_roca");
            Pintar();
        });
    }
}
