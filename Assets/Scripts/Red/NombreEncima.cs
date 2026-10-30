using UnityEngine;

/// Nombre de otro jugador flotando sobre su cabeza (tarea 7.3).
public class NombreEncima : MonoBehaviour
{
    public JugadorRed jugador;
    TextMesh _texto;

    void Start()
    {
        var go = new GameObject("Nombre");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        _texto = go.AddComponent<TextMesh>();
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _texto.font = fuente;
        go.GetComponent<MeshRenderer>().sharedMaterial = fuente.material;
        _texto.fontSize = 48; _texto.characterSize = 0.03f;
        _texto.anchor = TextAnchor.MiddleCenter; _texto.alignment = TextAlignment.Center;
        _texto.color = new Color(1f, 0.95f, 0.75f);
    }

    void LateUpdate()
    {
        if (!_texto) return;
        if (jugador) _texto.text = jugador.Nombre;
        var cam = Camera.main;
        if (cam) _texto.transform.rotation = cam.transform.rotation;
    }
}
