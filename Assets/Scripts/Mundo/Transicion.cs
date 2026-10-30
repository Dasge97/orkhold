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
    Text _consejo;

    static readonly string[] Consejos =
    {
        "En la mazmorra, si mueres pierdes la mitad de lo que llevabas.",
        "Junto a la fogata del pueblo recuperas vida.",
        "Los muros de piedra aguantan el doble que los de madera.",
        "Los defensores de tu fuerte atacan a quien entra a asaltarlo.",
        "El cofre de la plaza busca un fuerte de otro jugador para asaltar.",
        "Mantén pulsado el botón de correr para llegar antes.",
        "Los árboles y las rocas vuelven a dar recursos al cabo de un minuto.",
    };

    /// Verdadero mientras dura el fundido o la carga; el jugador no se mueve.
    public static bool Ocupada { get; private set; }

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
            _instancia._consejo = Texto(_instancia._carga.transform, "", 34, new Color(0.85f, 0.85f, 0.8f), new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.45f), FontStyle.Normal);
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
