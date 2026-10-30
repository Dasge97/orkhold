using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Caja de diálogo con los personajes del pueblo (15-interfaz.md §5): retrato, nombre, el texto letra a letra
/// (tocar la caja lo completa) y botones de opciones. Nada salta solo: se abre al hablar con el botón de interactuar
/// y se cierra con "Adiós" o al alejarse. Se crea sola y sobrevive a los cambios de escena.
public class Dialogo : MonoBehaviour
{
    const float LetrasPorSegundo = 55f;
    const int MaxOpciones = 3;

    static Dialogo _instancia;

    GameObject _caja;
    Image _retrato;
    Text _nombre, _oficio, _texto;
    readonly List<Button> _botones = new List<Button>();
    string _completo = "";
    float _letras;
    Transform _quien;
    float _alejarse;

    /// Abierto ahora mismo.
    public static bool Abierto => _instancia && _instancia._caja.activeSelf;
    /// Texto completo que se está diciendo; vacío si está cerrado (para los tests).
    public static string Visible => Abierto ? _instancia._completo : "";
    /// Quién habla ahora; vacío si está cerrado.
    public static string Hablante => Abierto ? _instancia._nombre.text : "";
    /// Lo último que se dijo, aunque ya se haya cerrado (para los tests).
    public static string Ultimo { get; private set; } = "";

    /// Abre la caja. "quien": el personaje que habla (se cierra si el jugador se aleja de él).
    /// "opciones": texto del botón y qué hace; "Adiós" se añade siempre al final.
    public static void Abrir(Transform quien, string nombre, string oficio, string retrato, string texto, params (string, Action)[] opciones)
    {
        var d = Instancia;
        d._quien = quien;
        d._alejarse = 3.2f;
        d._nombre.text = nombre;
        d._oficio.text = oficio;
        d._retrato.sprite = Ui.Sprite("Iconos/retrato-" + retrato);
        d._retrato.color = d._retrato.sprite ? Color.white : new Color(1f, 1f, 1f, 0f);
        d._completo = texto;
        d._letras = 0f;
        d._texto.text = "";
        Ultimo = nombre + ": " + texto;
        int n = 0;
        foreach (var (etiqueta, accion) in opciones)
        {
            if (n >= MaxOpciones - 1) break;
            d.PonerBoton(n++, etiqueta, () => { Cerrar(); accion?.Invoke(); }, Ui.Verde);
        }
        d.PonerBoton(n++, "Adiós", Cerrar, Ui.Gris);
        for (int i = n; i < d._botones.Count; i++) d._botones[i].gameObject.SetActive(false);
        d._caja.SetActive(true);
        Sonidos.Tocar("recoger", null, 0.6f);
    }

    public static void Cerrar()
    {
        if (!Abierto) return;
        _instancia._caja.SetActive(false);
        _instancia._quien = null;
    }

    static Dialogo Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = Ui.Lienzo("Dialogo", 860);
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<Dialogo>();
            _instancia.Montar(go.transform);
            return _instancia;
        }
    }

    void Montar(Transform lienzo)
    {
        // Abajo en el centro, entre el joystick y los botones de acción.
        var caja = Ui.Marco(lienzo, "Caja", "panel_beige", new Vector2(-70f, 40f), new Vector2(1000f, 300f), new Vector2(0.5f, 0f));
        caja.pivot = new Vector2(0.5f, 0f);
        _caja = caja.gameObject;
        caja.gameObject.AddComponent<Button>().onClick.AddListener(Completar);
        caja.GetComponent<Button>().transition = Selectable.Transition.None;

        var marco = Ui.Marco(caja, "Marco retrato", "panelInset_brown", new Vector2(130f, 0f), new Vector2(200f, 200f), new Vector2(0f, 0.5f));
        _retrato = Ui.Icono(marco, "", Vector2.zero, new Vector2(180f, 180f));

        _nombre = Ui.Titulo(caja, "", 40, new Vector2(260f, -42f), new Vector2(600f, 50f), TextAnchor.MiddleLeft);
        _nombre.rectTransform.anchorMin = _nombre.rectTransform.anchorMax = new Vector2(0f, 1f);
        _nombre.rectTransform.pivot = new Vector2(0f, 0.5f);
        _oficio = Ui.Texto(caja, "", 26, new Vector2(260f, -78f), new Vector2(600f, 30f), TextAnchor.MiddleLeft);
        _oficio.rectTransform.anchorMin = _oficio.rectTransform.anchorMax = new Vector2(0f, 1f);
        _oficio.rectTransform.pivot = new Vector2(0f, 0.5f);
        _oficio.color = new Color(0.45f, 0.32f, 0.2f);
        Destroy(_oficio.GetComponent<Shadow>());

        _texto = Ui.Texto(caja, "", 32, Vector2.zero, Vector2.zero, TextAnchor.UpperLeft);
        var rt = _texto.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(260f, 28f); rt.offsetMax = new Vector2(-300f, -100f);
        _texto.color = Ui.Tinta;
        _texto.verticalOverflow = VerticalWrapMode.Truncate;
        _texto.resizeTextForBestFit = true; _texto.resizeTextMinSize = 22; _texto.resizeTextMaxSize = 30;
        Destroy(_texto.GetComponent<Shadow>());

        for (int i = 0; i < MaxOpciones; i++)
        {
            var b = Ui.Boton(caja, "", Vector2.zero, new Vector2(250f, 74f), Ui.Verde, () => { }, 30);
            var brt = (RectTransform)b.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-150f, -62f - i * 86f);
            _botones.Add(b);
        }
        _caja.SetActive(false);
    }

    void PonerBoton(int i, string etiqueta, Action accion, Color color)
    {
        var b = _botones[i];
        b.gameObject.SetActive(true);
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(() => accion());
        b.GetComponentInChildren<Text>().text = etiqueta;
        b.GetComponent<Image>().color = color == Ui.Gris ? new Color(0.75f, 0.7f, 0.62f) : new Color(0.55f, 0.85f, 0.5f);
    }

    void Completar() => _letras = _completo.Length;

    void Update()
    {
        if (!_caja.activeSelf) return;
        if (_letras < _completo.Length)
        {
            _letras = Mathf.Min(_completo.Length, _letras + Time.unscaledDeltaTime * LetrasPorSegundo);
            _texto.text = _completo.Substring(0, (int)_letras);
        }
        else if (_texto.text.Length != _completo.Length) _texto.text = _completo;
        // Si el jugador se va, la conversación se acaba.
        var yo = ControlJugador.Local;
        if (_quien && yo)
        {
            Vector3 d = yo.transform.position - _quien.transform.position; d.y = 0f;
            if (d.magnitude > _alejarse) Cerrar();
        }
        if (Transicion.Ocupada) Cerrar();
    }
}
