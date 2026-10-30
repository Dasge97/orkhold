using UnityEngine;
using UnityEngine.UI;

/// Aviso de botín del HUD (15-interfaz.md §3): "+3 madera" en dorado con su icono, abajo en el centro.
/// Entra, se queda 1,6 s y se desvanece; los nuevos empujan a los viejos hacia arriba.
public class AvisoBotin : MonoBehaviour
{
    const float Vida = 2.2f;

    public Recurso recurso;
    int _cantidad;
    float _t;
    Text _texto;
    CanvasGroup _grupo;

    public bool Vivo => _t < Vida - 0.5f;

    public static AvisoBotin Crear(RectTransform padre, Recurso r, int n)
    {
        var fondo = Ui.Marco(padre, "Botin", "buttonLong_brown", Vector2.zero, new Vector2(300f, 64f), new Vector2(0.5f, 0f));
        fondo.pivot = new Vector2(0.5f, 0f);
        fondo.GetComponent<Image>().raycastTarget = false;
        fondo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.92f);
        var a = fondo.gameObject.AddComponent<AvisoBotin>();
        a._grupo = fondo.gameObject.AddComponent<CanvasGroup>();
        a._grupo.blocksRaycasts = false;
        a.recurso = r;
        Ui.Icono(fondo, "Iconos/" + HUD.NombreIcono(r), new Vector2(-104f, 2f), new Vector2(52f, 52f));
        a._texto = Ui.Texto(fondo, "", 32, new Vector2(30f, 2f), new Vector2(220f, 50f), TextAnchor.MiddleLeft);
        a._texto.font = Ui.FuenteTitulo; a._texto.color = Ui.Dorado;
        Ui.Contorno(a._texto, 1.5f);
        // Los que ya estaban suben un hueco.
        foreach (Transform hijo in padre)
            if (hijo != fondo) ((RectTransform)hijo).anchoredPosition += new Vector2(0f, 72f);
        a.Sumar(n);
        return a;
    }

    public void Sumar(int n)
    {
        _cantidad += n;
        _texto.text = $"+{_cantidad} {HUD.NombreRecurso(recurso, _cantidad)}";
        _t = Mathf.Min(_t, 0.2f);
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        float entra = Mathf.Clamp01(_t / 0.2f);
        _grupo.alpha = entra * (1f - Mathf.Clamp01((_t - (Vida - 0.5f)) / 0.5f));
        transform.localScale = Vector3.one * (0.85f + 0.15f * entra);
        if (_t >= Vida) Destroy(gameObject);
    }
}
