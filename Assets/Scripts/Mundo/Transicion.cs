using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Cambio de escena con fundido: a negro en 0,5 s, carga asíncrona y vuelta en 0,5 s.
/// Mientras carga, la pantalla negra hace de pantalla de carga: el nombre del juego y un consejo (tarea 9.1).
/// Se crea sola la primera vez que se usa y sobrevive a los cambios de escena.
public class Transicion : MonoBehaviour
{
    public const float Duracion = 0.5f;

    static Transicion _instancia;
    CanvasGroup _grupo;
    GameObject _carga;
    Text _consejo, _lugar;

    static readonly string[] Consejos =
    {
        "La Brasa lleva cien años encendida. Nadie la vigila. Nadie hace falta.",
        "Un orco sin rey sirve a quien le paga. Un orco con rey, también.",
        "La Ley del Vado: si encuentran tu escondite, pierdes una parte. Si encuentras el suyo, ganas una parte. Nadie pierde el fuerte.",
        "Los muros de piedra aguantan el doble. También cuestan el doble de golpes.",
        "Un cofre vacío también es un escondite: quien lo marca, pierde una acción.",
        "Urzog lleva cien años abajo. No tiene prisa. Tú sí.",
        "Ansel dice que a las ruinas les faltan puertas por encontrar. Ansel dice muchas cosas.",
        "Consejo: los Cazadores del Vado huyen si te acercas.",
    };

    /// Verdadero mientras dura el fundido o la carga; el jugador no se mueve.
    public static bool Ocupada { get; private set; }
    /// Nombre del lugar en la última pantalla de carga ("Vado Gris" al ir al pueblo), para los tests.
    public static string Lugar => _instancia && _instancia._lugar ? _instancia._lugar.text : "";

    /// Se dispara cuando la escena nueva ya está cargada (antes del fundido de vuelta).
    public static event System.Action<string> AlLlegar;

    static Transicion Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = new GameObject("Transicion");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<Transicion>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            _instancia._grupo = go.AddComponent<CanvasGroup>();
            _instancia._grupo.alpha = 0f;
            _instancia._grupo.blocksRaycasts = false;
            var negro = new GameObject("Negro").AddComponent<Image>();
            negro.transform.SetParent(go.transform, false);
            negro.color = Color.black;
            var rt = negro.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var esc = go.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920f, 1080f);
            esc.matchWidthOrHeight = 0.5f;
            // Pantalla de carga: nombre y consejo, solo en los cambios de escena.
            _instancia._carga = new GameObject("Carga");
            _instancia._carga.transform.SetParent(go.transform, false);
            var crt = _instancia._carga.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = crt.offsetMax = Vector2.zero;
            Texto(_instancia._carga.transform, "ORKHOLD", 96, new Color(1f, 0.86f, 0.45f), new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.68f), FontStyle.Bold);
            _instancia._lugar = Texto(_instancia._carga.transform, "", 44, new Color(0.9f, 0.8f, 0.6f), new Vector2(0.1f, 0.44f), new Vector2(0.9f, 0.52f), FontStyle.Italic);
            _instancia._consejo = Texto(_instancia._carga.transform, "", 34, new Color(0.85f, 0.85f, 0.8f), new Vector2(0.15f, 0.26f), new Vector2(0.85f, 0.42f), FontStyle.Normal);
            _instancia._carga.SetActive(false);
            return _instancia;
        }
    }

    /// Va a la escena con fundido. "esperaEnNegro": segundos de pantalla negra antes de cargar
    /// (al morir). Si ya hay una transición en marcha, no hace nada.
    public static void Ir(string escena, float esperaEnNegro = 0f)
    {
        if (Ocupada) return;
        Instancia.StartCoroutine(Instancia.Cambiar(escena, esperaEnNegro));
    }

    /// Fundido a negro, espera y vuelta sin cambiar de escena (muerte, reaparición).
    public static void Fundido(System.Action enNegro, float espera = 0f)
    {
        if (Ocupada) return;
        Instancia.StartCoroutine(Instancia.SoloFundido(enNegro, espera));
    }

    static Text Texto(Transform padre, string texto, int tam, Color color, Vector2 min, Vector2 max, FontStyle estilo)
    {
        var t = new GameObject("Texto").AddComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = texto; t.fontSize = tam; t.color = color; t.fontStyle = estilo;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        var rt = t.rectTransform;
        rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    IEnumerator Cambiar(string escena, float espera)
    {
        Ocupada = true;
        _grupo.blocksRaycasts = true;
        _consejo.text = Consejos[Random.Range(0, Consejos.Length)];
        // Al pueblo: su nombre debajo del título (07-lore.md §7).
        if (_lugar) _lugar.text = escena == "Pueblo" ? "Vado Gris" : escena == "Mazmorra1" ? "Las catacumbas" : escena == "Mazmorra2" ? "La Forja Hundida"
            : escena.StartsWith("Mazmorra3") ? "La Torre del Vigía" : escena == "Mazmorra4" ? "El Pozo de los Clanes" : "";
        _carga.SetActive(true);
        yield return Fundir(0f, 1f);
        if (espera > 0f) yield return new WaitForSecondsRealtime(espera);
        Viaje.escenaAnterior = SceneManager.GetActiveScene().name;
        var op = SceneManager.LoadSceneAsync(escena);
        while (op != null && !op.isDone) yield return null;
        yield return null;
        AlLlegar?.Invoke(escena);
        yield return Fundir(1f, 0f);
        _carga.SetActive(false);
        _grupo.blocksRaycasts = false;
        Ocupada = false;
    }

    IEnumerator SoloFundido(System.Action enNegro, float espera)
    {
        Ocupada = true;
        _grupo.blocksRaycasts = true;
        yield return Fundir(0f, 1f);
        enNegro?.Invoke();
        if (espera > 0f) yield return new WaitForSecondsRealtime(espera);
        yield return Fundir(1f, 0f);
        _grupo.blocksRaycasts = false;
        Ocupada = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        float t = 0f;
        while (t < Duracion)
        {
            t += Time.unscaledDeltaTime;
            _grupo.alpha = Mathf.Lerp(desde, hasta, t / Duracion);
            yield return null;
        }
        _grupo.alpha = hasta;
    }
}
