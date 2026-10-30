using UnityEngine;
using UnityEngine.UI;

/// Mensaje corto en el centro de la pantalla ("Has muerto", "Vida recuperada"...).
/// Se crea solo y sobrevive a los cambios de escena.
public class Avisos : MonoBehaviour
{
    static Avisos _instancia;
    Text _texto;
    CanvasGroup _grupo;
    float _hasta;

    /// Último aviso mostrado (para los tests).
    public static string Ultimo { get; private set; } = "";

    static Avisos Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = new GameObject("Avisos");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var esc = go.AddComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            esc.referenceResolution = new Vector2(1920f, 1080f);
            esc.matchWidthOrHeight = 0.5f;
            _instancia = go.AddComponent<Avisos>();
            _instancia._grupo = go.AddComponent<CanvasGroup>();
            _instancia._grupo.blocksRaycasts = false;
            _instancia._grupo.alpha = 0f;
            var t = new GameObject("Texto").AddComponent<Text>();
            t.transform.SetParent(go.transform, false);
            t.font = Ui.Fuente;
            t.fontSize = 56; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.62f); rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            t.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);
            _instancia._texto = t;
            return _instancia;
        }
    }

    public static void Mostrar(string texto, float segundos = 2f)
    {
        Ultimo = texto;
        var a = Instancia;
        a._texto.text = texto;
        a._hasta = Time.unscaledTime + segundos;
        a._grupo.alpha = 1f;
    }

    void Update()
    {
        if (_grupo.alpha <= 0f) return;
        _grupo.alpha = Mathf.Clamp01((_hasta - Time.unscaledTime) / 0.4f);
    }
}
