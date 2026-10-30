using UnityEngine;
using UnityEngine.UI;

/// Cartel del tutorial (tarea 9.2): cuando el jugador de este móvil se acerca, su texto sale en un
/// recuadro arriba de la pantalla y se va al alejarse. Los textos vienen de la cabecera del mapa
/// (cartel1 ... cartel5 en pueblo.txt); los coloca MontarEscenas.
public class Cartel : MonoBehaviour
{
    [TextArea] public string texto = "";
    public float radio = 2.4f;

    static Cartel _mostrado;
    static CanvasGroup _grupo;
    static Text _texto;

    /// Texto que se ve ahora (para los tests); vacío si no hay ninguno.
    public static string Visible => _grupo && _grupo.alpha > 0.5f && _mostrado ? _mostrado.texto : "";

    void Update()
    {
        var yo = ControlJugador.Local;
        bool cerca = false;
        if (yo)
        {
            Vector3 d = yo.transform.position - transform.position; d.y = 0f;
            cerca = d.sqrMagnitude <= radio * radio;
        }
        if (cerca && _mostrado != this) { _mostrado = this; Panel(); _texto.text = texto; }
        if (_mostrado != this) return;
        if (!cerca && _mostrado == this && _grupo && _grupo.alpha <= 0f) { _mostrado = null; return; }
        _grupo.alpha = Mathf.MoveTowards(_grupo.alpha, cerca ? 1f : 0f, Time.unscaledDeltaTime * 4f);
    }

    void OnDestroy()
    {
        if (_mostrado == this) { _mostrado = null; if (_grupo) _grupo.alpha = 0f; }
    }

    /// Recuadro oscuro arriba en el centro, debajo del HUD. Se crea una vez y sobrevive a los cambios de escena.
    static void Panel()
    {
        if (_grupo) return;
        var go = new GameObject("Cartel tutorial");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 850;
        var esc = go.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        _grupo = go.AddComponent<CanvasGroup>();
        _grupo.blocksRaycasts = false;
        _grupo.alpha = 0f;

        var fondo = new GameObject("Fondo").AddComponent<Image>();
        fondo.transform.SetParent(go.transform, false);
        fondo.color = new Color(0.08f, 0.07f, 0.05f, 0.82f);
        var rf = fondo.rectTransform;
        rf.anchorMin = new Vector2(0.22f, 0.66f); rf.anchorMax = new Vector2(0.78f, 0.84f);
        rf.offsetMin = rf.offsetMax = Vector2.zero;
        fondo.gameObject.AddComponent<Outline>().effectColor = new Color(0.85f, 0.7f, 0.35f, 0.9f);

        _texto = new GameObject("Texto").AddComponent<Text>();
        _texto.transform.SetParent(fondo.transform, false);
        _texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _texto.fontSize = 36;
        _texto.alignment = TextAnchor.MiddleCenter;
        _texto.color = new Color(1f, 0.95f, 0.85f);
        _texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        _texto.verticalOverflow = VerticalWrapMode.Truncate;
        _texto.resizeTextForBestFit = true;
        _texto.resizeTextMinSize = 24; _texto.resizeTextMaxSize = 36;
        var rt = _texto.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(28f, 14f); rt.offsetMax = new Vector2(-28f, -14f);
    }
}
