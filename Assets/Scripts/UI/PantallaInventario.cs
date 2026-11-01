using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Inventario (petición de Daniel del 31-10-2026): lo que llevas encima, en casillas con icono, nombre y cantidad.
/// Se abre con el botón del cofre del HUD y se cierra con "Cerrar" o tocando fuera. Se crea sola la primera vez.
public class PantallaInventario : MonoBehaviour
{
    static readonly Recurso[] Orden = { Recurso.Moneda, Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso, Recurso.Pocion };

    static PantallaInventario _instancia;
    GameObject _velo;
    readonly Dictionary<Recurso, Text> _cantidades = new Dictionary<Recurso, Text>();

    public static bool Abierta => _instancia && _instancia._velo.activeSelf;

    /// Cantidad que se ve en la casilla de un recurso (para los tests).
    public static string Cantidad(Recurso r) => _instancia && _instancia._cantidades.TryGetValue(r, out var t) ? t.text : "";

    public static void Abrir()
    {
        if (!_instancia)
        {
            var go = Ui.Lienzo("Inventario", 870);
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<PantallaInventario>();
            _instancia.Montar(go.transform);
            Partida.AlCambiar += _instancia.Refrescar;
        }
        _instancia.Refrescar();
        _instancia._velo.SetActive(true);
        Sonidos.Tocar("recoger", null, 0.6f);
    }

    public static void Cerrar() { if (_instancia) _instancia._velo.SetActive(false); }

    void OnDestroy() { Partida.AlCambiar -= Refrescar; }

    void Montar(Transform lienzo)
    {
        var panel = Ui.PanelCentrado(lienzo, new Vector2(1060f, 700f), out _velo);
        // Tocar fuera del panel lo cierra; tocar dentro, no.
        _velo.AddComponent<Button>().onClick.AddListener(Cerrar);
        _velo.GetComponent<Button>().transition = Selectable.Transition.None;
        panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

        Ui.Titulo(panel, "Inventario", 54, new Vector2(0f, 285f), new Vector2(900f, 70f));
        for (int i = 0; i < Orden.Length; i++)
        {
            var r = Orden[i];
            var casilla = Ui.Marco(panel, "Casilla " + r, "panelInset_beige", new Vector2(-320f + (i % 3) * 320f, 105f - (i / 3) * 250f), new Vector2(290f, 230f));
            Ui.Icono(casilla, "Iconos/" + HUD.NombreIcono(r), new Vector2(0f, 30f), new Vector2(130f, 130f));
            var cantidad = Ui.Titulo(casilla, "0", 58, new Vector2(70f, -22f), new Vector2(140f, 70f), TextAnchor.MiddleRight);
            _cantidades[r] = cantidad;
            var nombre = Ui.Texto(casilla, Nombre(r), 30, new Vector2(0f, -82f), new Vector2(270f, 40f));
            nombre.color = Ui.Tinta;
            Destroy(nombre.GetComponent<Shadow>());
        }
        Ui.Boton(panel, "Cerrar", new Vector2(0f, -290f), new Vector2(320f, 84f), Ui.Gris, Cerrar);
        _velo.SetActive(false);
    }

    static string Nombre(Recurso r) =>
        r == Recurso.Moneda ? "Monedas" : r == Recurso.Madera ? "Madera" : r == Recurso.Piedra ? "Piedra"
        : r == Recurso.Hierro ? "Hierro" : r == Recurso.Hueso ? "Huesos" : "Pociones";

    void Refrescar()
    {
        var inv = Partida.Inventario;
        foreach (var kv in _cantidades) kv.Value.text = inv[kv.Key].ToString();
    }
}
