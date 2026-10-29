using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Cambio de escena con fundido: a negro en 0,5 s, carga asíncrona y vuelta en 0,5 s.
/// Se crea sola la primera vez que se usa y sobrevive a los cambios de escena.
public class Transicion : MonoBehaviour
{
    public const float Duracion = 0.5f;

    static Transicion _instancia;
    CanvasGroup _grupo;

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
            return _instancia;
        }
    }

    /// Va a la escena con fundido. Si ya hay una transición en marcha, no hace nada.
    public static void Ir(string escena)
    {
        if (Ocupada) return;
        Instancia.StartCoroutine(Instancia.Cambiar(escena));
    }

    /// Fundido a negro, espera y vuelta sin cambiar de escena (muerte, reaparición).
    public static void Fundido(System.Action enNegro, float espera = 0f)
    {
        if (Ocupada) return;
        Instancia.StartCoroutine(Instancia.SoloFundido(enNegro, espera));
    }

    IEnumerator Cambiar(string escena)
    {
        Ocupada = true;
        _grupo.blocksRaycasts = true;
        yield return Fundir(0f, 1f);
        Viaje.escenaAnterior = SceneManager.GetActiveScene().name;
        var op = SceneManager.LoadSceneAsync(escena);
        while (op != null && !op.isDone) yield return null;
        yield return null;
        AlLlegar?.Invoke(escena);
        yield return Fundir(1f, 0f);
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
