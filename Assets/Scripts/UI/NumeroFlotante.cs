using UnityEngine;

/// Texto en el mundo que sube y se desvanece (daño, curación, recursos recogidos).
public class NumeroFlotante : MonoBehaviour
{
    const float Duracion = 0.9f;
    TextMesh _texto;
    Color _color;
    float _t;

    static Font _fuente;

    public static void Crear(Vector3 donde, string texto, Color color, float tamano = 1f)
    {
        if (!_fuente) _fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var go = new GameObject("Numero " + texto);
        go.transform.position = donde;
        var tm = go.AddComponent<TextMesh>();
        tm.font = _fuente;
        tm.GetComponent<MeshRenderer>().sharedMaterial = _fuente.material;
        tm.text = texto;
        tm.fontSize = 64;
        tm.characterSize = 0.035f * tamano;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        var n = go.AddComponent<NumeroFlotante>();
        n._texto = tm; n._color = color;
    }

    void LateUpdate()
    {
        _t += Time.deltaTime;
        transform.position += Vector3.up * (0.9f * Time.deltaTime);
        var cam = Camera.main;
        if (cam) transform.rotation = cam.transform.rotation;
        var c = _color; c.a = 1f - Mathf.Clamp01((_t - 0.4f) / (Duracion - 0.4f));
        _texto.color = c;
        if (_t >= Duracion) Destroy(gameObject);
    }
}
