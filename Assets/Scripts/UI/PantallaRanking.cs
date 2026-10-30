using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// Ranking (50 mejores y tu puesto) e historial (últimos 10 asaltos hechos y recibidos),
/// desde el tablón del pueblo (01-diseno.md §6.5).
public class PantallaRanking : MonoBehaviour
{
    static PantallaRanking _instancia;
    GameObject _velo;
    Text _ranking, _historial, _estado;

    public static bool Abierta => _instancia && _instancia._velo.activeSelf;
    /// Texto mostrado (para los tests).
    public static string TextoRanking => _instancia ? _instancia._ranking.text : "";
    public static string TextoHistorial => _instancia ? _instancia._historial.text : "";

    public static void Abrir()
    {
        if (!_instancia) Crear();
        _instancia._velo.SetActive(true);
        _instancia.Cargar();
    }

    public static void Cerrar() { if (_instancia) _instancia._velo.SetActive(false); }

    static void Crear()
    {
        var go = Ui.Lienzo("PantallaRanking", 650);
        _instancia = go.AddComponent<PantallaRanking>();
        var panel = Ui.PanelCentrado(go.transform, new Vector2(1700f, 940f), out _instancia._velo);
        Ui.Texto(panel, "Ranking", 52, new Vector2(-420f, 400f), new Vector2(760f, 70f), negrita: true);
        Ui.Texto(panel, "Historial de asaltos", 52, new Vector2(420f, 400f), new Vector2(760f, 70f), negrita: true);
        _instancia._ranking = Ui.Texto(panel, "", 30, new Vector2(-420f, -20f), new Vector2(760f, 740f), TextAnchor.UpperLeft);
        _instancia._historial = Ui.Texto(panel, "", 28, new Vector2(420f, -20f), new Vector2(760f, 740f), TextAnchor.UpperLeft);
        _instancia._estado = Ui.Texto(panel, "", 32, new Vector2(0f, -400f), new Vector2(1000f, 50f));
        Ui.Boton(panel, "Cerrar", new Vector2(640f, -405f), new Vector2(260f, 80f), Ui.Rojo, Cerrar, 36);
    }

    void Cargar()
    {
        if (!Sesion.Conectado)
        {
            _ranking.text = $"Sin servidor: el ranking necesita conexión.\n\nTu puntuación: <b>{Partida.Actual.puntuacion}</b>";
            _historial.text = "";
            _estado.text = Configuracion.ModoLocal ? "Modo local" : "Sin conexión";
            return;
        }
        _estado.text = "Cargando...";
        ApiCliente.Get("/ranking", r =>
        {
            if (!r.Ok) { _ranking.text = r.Mensaje; return; }
            var d = r.Leer<MensajesApi.RespuestaRanking>();
            var sb = new StringBuilder();
            foreach (var f in d.mejores ?? new List<MensajesApi.FilaRanking>())
            {
                if (f.puesto > 18) break;
                bool yo = f.id == Sesion.JugadorId;
                sb.AppendLine($"{(yo ? "<color=#f2c040>" : "")}{f.puesto,3}. {f.nombre}  —  {f.puntuacion}{(yo ? "</color>" : "")}");
            }
            sb.AppendLine($"\nTu puesto: <b>{d.tu_puesto}</b> con <b>{d.tu_puntuacion}</b> puntos");
            _ranking.text = sb.ToString();
            _estado.text = "";
        });
        ApiCliente.Get("/asaltos/historial", r =>
        {
            if (!r.Ok) { _historial.text = r.Mensaje; return; }
            var d = r.Leer<MensajesApi.RespuestaHistorial>();
            var sb = new StringBuilder("<b>Hechos</b>\n");
            Lineas(sb, d.hechos, true);
            sb.Append("\n<b>Recibidos</b>\n");
            Lineas(sb, d.recibidos, false);
            _historial.text = sb.ToString();
        });
    }

    static void Lineas(StringBuilder sb, List<MensajesApi.FilaHistorial> filas, bool hechos)
    {
        if (filas == null || filas.Count == 0) { sb.AppendLine("  (ninguno)"); return; }
        foreach (var f in filas)
        {
            // Para el defensor, ganar el atacante es perder.
            bool bien = hechos ? f.ganado : !f.ganado;
            string que = hechos ? (f.ganado ? "Ganaste a" : "Perdiste contra") : (f.ganado ? "Te asaltó y ganó" : "Te asaltó y perdió");
            string puntos = hechos ? $" ({(f.puntos >= 0 ? "+" : "")}{f.puntos})" : "";
            sb.AppendLine($"  <color={(bien ? "#7fe08a" : "#ff8a80")}>{que}</color> {f.rival} · {f.segundos} s · {f.muros_rotos} muros{puntos}");
        }
    }
}
