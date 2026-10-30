using UnityEngine;
using UnityEngine.UI;

/// Botón de menú arriba a la derecha y su panel (01-diseno.md §6.6): sonido, vibración y
/// reiniciar la partida con confirmación. Se monta solo al arrancar la escena.
public class Menu : MonoBehaviour
{
    public Sprite icono;

    GameObject _panel, _confirmar;
    Text _sonido, _vibracion;
    static Font _fuente;

    public bool Abierto => _panel && _panel.activeSelf;

    void Start()
    {
        if (!_fuente) _fuente = Ui.Fuente;
        var b = Caja("BotonMenu", transform, new Vector2(1f, 1f), new Vector2(-90f, -80f), new Vector2(120f, 120f), new Color(0f, 0f, 0f, 0.45f));
        var fondo = b.GetComponent<Image>();
        fondo.sprite = Ui.Sprite("buttonSquare_brown"); fondo.type = Image.Type.Sliced; fondo.pixelsPerUnitMultiplier = 0.5f;
        fondo.color = Color.white;
        b.gameObject.AddComponent<Button>().onClick.AddListener(Abrir);
        if (icono)
        {
            var ic = Caja("Icono", b, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f), Color.white);
            ic.GetComponent<Image>().sprite = icono;
            ic.GetComponent<Image>().raycastTarget = false;
        }

        _panel = PanelCentrado("PanelMenu", new Vector2(760f, 640f));
        Texto(_panel.transform, "Menú", 56, new Vector2(0f, 240f));
        _sonido = BotonTexto(_panel.transform, "", new Vector2(0f, 120f), () => { Ajustes.Sonido = !Ajustes.Sonido; Refrescar(); });
        _vibracion = BotonTexto(_panel.transform, "", new Vector2(0f, 10f), () => { Ajustes.Vibracion = !Ajustes.Vibracion; Refrescar(); });
        // Con servidor, la partida es la cuenta: en vez de reiniciar, la ventana de la cuenta (14-cuentas.md §5).
        if (Configuracion.ModoLocal) BotonTexto(_panel.transform, "Reiniciar partida", new Vector2(0f, -100f), () => _confirmar.SetActive(true));
        else BotonTexto(_panel.transform, "Cuenta", new Vector2(0f, -100f), () => { Cerrar(); PantallaCuenta.Abrir(PantallaCuenta.Modo.Cuenta); });
        BotonTexto(_panel.transform, "Cerrar", new Vector2(0f, -210f), Cerrar);
        _panel.SetActive(false);

        _confirmar = PanelCentrado("Confirmar", new Vector2(900f, 420f));
        Texto(_confirmar.transform, "¿Reiniciar la partida?\nSe borran inventario, fuerte y progreso.", 40, new Vector2(0f, 90f));
        BotonTexto(_confirmar.transform, "Sí, reiniciar", new Vector2(-200f, -100f), Reiniciar, 340f);
        BotonTexto(_confirmar.transform, "No", new Vector2(200f, -100f), () => _confirmar.SetActive(false), 340f);
        _confirmar.SetActive(false);
        Refrescar();
    }

    public void Abrir() { _panel.SetActive(true); Refrescar(); }
    public void Cerrar() { _panel.SetActive(false); _confirmar.SetActive(false); }

    public void Reiniciar()
    {
        Cerrar();
        Partida.Reemplazar(new DatosPartida());
        Guardado.Guardar();
        Viaje.llegarA = "";
        Avisos.Mostrar("Partida nueva", 2f);
        Transicion.Ir("Pueblo");
    }

    void Refrescar()
    {
        if (_sonido) _sonido.text = "Sonido: " + (Ajustes.Sonido ? "sí" : "no");
        if (_vibracion) _vibracion.text = "Vibración: " + (Ajustes.Vibracion ? "sí" : "no");
    }

    // ---------- piezas de interfaz ----------

    GameObject PanelCentrado(string nombre, Vector2 tam)
    {
        var velo = Caja(nombre, transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f));
        velo.anchorMin = Vector2.zero; velo.anchorMax = Vector2.one; velo.offsetMin = velo.offsetMax = Vector2.zero;
        Caja("Fondo", velo, new Vector2(0.5f, 0.5f), Vector2.zero, tam, new Color(0.1f, 0.11f, 0.15f, 0.97f));
        return velo.gameObject;
    }

    Text BotonTexto(Transform padre, string texto, Vector2 pos, UnityEngine.Events.UnityAction accion, float ancho = 560f)
    {
        var b = Caja("Boton " + texto, padre, new Vector2(0.5f, 0.5f), pos, new Vector2(ancho, 90f), new Color(0.22f, 0.45f, 0.3f, 1f));
        b.gameObject.AddComponent<Button>().onClick.AddListener(accion);
        return Texto(b, texto, 40, Vector2.zero);
    }

    Text Texto(Transform padre, string texto, int tam, Vector2 pos)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = _fuente; t.fontSize = tam; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false; t.text = texto;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = t.rectTransform;
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(860f, 160f);
        return t;
    }

    static RectTransform Caja(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam, Color color)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        var rt = g.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancla; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        g.AddComponent<Image>().color = color;
        return rt;
    }
}
