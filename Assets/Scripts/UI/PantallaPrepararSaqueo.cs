using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// "Preparar saqueo" (08-formato-y-pvp.md §4.1): rival (nombre, puntos, estandarte), cuántas piezas y escondites
/// tiene a la vista (no dónde), la tasa y hasta 2 herramientas. "Saquear" paga y entra en su fuerte.
public class PantallaPrepararSaqueo : MonoBehaviour
{
    public static PantallaPrepararSaqueo Abierta { get; private set; }
    /// Solo para los tests contra la API local en modo de pruebas: pedir un rival por nombre.
    public static string RivalPedido = "";
    [System.Serializable] class PeticionPreparar { public string rival; }

    public MensajesApi.RespuestaPreparar Preparado { get; private set; }
    public string Mensaje => _mensaje ? _mensaje.text : "";
    public readonly List<string> Elegidas = new List<string>();

    RectTransform _panel;
    GameObject _velo, _contenido;
    Text _mensaje;
    bool _ocupada;

    public static PantallaPrepararSaqueo Abrir()
    {
        if (Abierta) Destroy(Abierta.gameObject);
        var go = Ui.Lienzo("PantallaPrepararSaqueo", 800);
        var p = go.AddComponent<PantallaPrepararSaqueo>();
        Abierta = p;
        p._panel = Ui.PanelCentrado(go.transform, new Vector2(1300f, 940f), out p._velo);
        Ui.Texto(p._panel, "Preparar saqueo", 60, new Vector2(0f, 400f), new Vector2(1200f, 80f), negrita: true).color = Ui.Dorado;
        p._mensaje = Ui.Texto(p._panel, "Buscando rival...", 34, new Vector2(0f, -310f), new Vector2(1200f, 70f));
        Ui.Boton(p._panel, "Cancelar", new Vector2(420f, -400f), new Vector2(340f, 90f), Ui.Gris, p.Cerrar, 36);
        p.Buscar();
        return p;
    }

    void OnDestroy() { if (Abierta == this) Abierta = null; }

    public void Cerrar() => Destroy(gameObject);

    /// POST /saqueo/preparar (también "Otro rival").
    public void Buscar()
    {
        if (_ocupada) return;
        _ocupada = true;
        _mensaje.text = "Buscando rival...";
        ApiCliente.Post("/saqueo/preparar", new PeticionPreparar { rival = RivalPedido }, r =>
        {
            _ocupada = false;
            if (this == null) return;
            if (!r.Ok) { _mensaje.text = r.Mensaje; return; }
            Preparado = r.Leer<MensajesApi.RespuestaPreparar>();
            Elegidas.Clear();
            _mensaje.text = "";
            Montar();
        });
    }

    void Montar()
    {
        if (_contenido) Destroy(_contenido);
        _contenido = new GameObject("Contenido", typeof(RectTransform));
        _contenido.transform.SetParent(_panel, false);
        var c = _contenido.transform;
        var p = Preparado;
        var rv = p.rival;
        string tramo = Saqueo.NombreTramo(rv.estandarte);
        Ui.Texto(c, $"<b>{rv.nombre}</b>  ·  {rv.puntuacion} puntos" + (tramo.Length > 0 ? $"  ·  estandarte {tramo}" : ""), 44,
            new Vector2(0f, 300f), new Vector2(1200f, 70f));
        var sb = new StringBuilder();
        sb.AppendLine($"Su fuerte tiene {rv.piezas} piezas y {rv.escondites} escondites a la vista. Puede haber más enterrados.");
        sb.AppendLine(rv.prueba ? "No hay rivales a tu altura ahora: es un fuerte de prueba, sin botín real, sin tasa y sin puntos."
                                : $"Tasa: {p.tasa} monedas. Si no encuentras nada, se las queda el defensor.");
        sb.Append($"Saqueos hoy: {p.saqueos_hoy}/{p.max_saqueos_dia}");
        Ui.Texto(c, sb.ToString(), 32, new Vector2(0f, 175f), new Vector2(1180f, 170f));

        Ui.Texto(c, p.herramientas.Count > 0 ? $"Herramientas (hasta {p.max_herramientas}):" : "Sin herramientas. Ferra, la herrera, fabrica faroles.", 32,
            new Vector2(0f, 60f), new Vector2(1180f, 50f));
        for (int i = 0; i < p.herramientas.Count; i++)
        {
            var h = p.herramientas[i];
            string id = h.id;
            var b = Ui.Boton(c, $"{NombreHerramienta(id)} ({h.cantidad})", new Vector2(-360f + (i % 3) * 360f, -30f - (i / 3) * 100f),
                new Vector2(340f, 86f), Ui.Gris, () => Alternar(id), 32);
            b.name = "Herramienta " + id;
        }
        Ui.Boton(c, "Saquear", new Vector2(-420f, -400f), new Vector2(340f, 100f), Ui.Rojo, Saquear, 44);
        Ui.Boton(c, "Otro rival", new Vector2(0f, -400f), new Vector2(340f, 90f), Ui.Gris, Buscar, 36);
        Pintar();
    }

    public void Alternar(string id)
    {
        if (Elegidas.Contains(id)) Elegidas.Remove(id);
        else if (Elegidas.Count < Preparado.max_herramientas) Elegidas.Add(id);
        else _mensaje.text = $"Como mucho {Preparado.max_herramientas} herramientas";
        Pintar();
    }

    void Pintar()
    {
        foreach (var b in _contenido.GetComponentsInChildren<Button>())
            if (b.name.StartsWith("Herramienta "))
                b.GetComponent<Image>().color = Elegidas.Contains(b.name.Substring(12)) ? Ui.Dorado * 0.85f : Ui.Gris;
    }

    /// POST /saqueo/empezar: paga la tasa y las herramientas y entra en el fuerte del rival.
    public void Saquear()
    {
        if (_ocupada || Preparado == null) return;
        _ocupada = true;
        _mensaje.text = "Entrando...";
        ApiCliente.Post("/saqueo/empezar", new MensajesApi.PeticionEmpezar { saqueo_id = Preparado.saqueo_id, herramientas = new List<string>(Elegidas) }, r =>
        {
            _ocupada = false;
            if (this == null) return;
            if (!r.Ok) { _mensaje.text = r.Mensaje; return; }
            var e = r.Leer<MensajesApi.RespuestaEmpezar>();
            Saqueo.Datos = e;
            Saqueo.Rival = Preparado.rival;
            Fuerte.DisenoAsalto = e.diseno;
            Sincronizacion.AplicarJugador(e.jugador);
            Cerrar();
            Viaje.llegarA = "";
            Sonidos.Tocar("puerta");
            Transicion.Ir("Asalto");
        });
    }

    public static string NombreHerramienta(string id)
    {
        switch (id)
        {
            case "farol": return "Farol";
            case "cuarta_accion": return "Cuarta acción";
            case "pico": return "Pico";
            case "bomba": return "Bomba";
            case "sabueso": return "Sabueso";
            case "senuelo": return "Señuelo";
            default: return id;
        }
    }
}
