using UnityEngine;

/// Al recibir daño: parpadeo rojo de 0,15 s y número flotante. Al curarse: número verde.
[RequireComponent(typeof(Vida))]
public class EfectoDano : MonoBehaviour
{
    public const float DuracionParpadeo = 0.15f;
    public float alturaNumero = 1f;

    Vida _vida;
    Renderer[] _renderers;
    Material[][] _originales;
    float _hasta;
    bool _rojo;

    static Material _materialRojo;

    /// Verdadero mientras parpadea (para los tests).
    public bool Parpadeando => _rojo;

    void Awake()
    {
        _vida = GetComponent<Vida>();
        _renderers = GetComponentsInChildren<Renderer>();
        _originales = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++) _originales[i] = _renderers[i].sharedMaterials;
        if (!_materialRojo) _materialRojo = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.15f, 0.1f), name = "Parpadeo rojo" };
        _vida.AlRecibirDano += (n, desde) =>
        {
            Rojo(true);
            _hasta = Time.time + DuracionParpadeo;
            NumeroFlotante.Crear(transform.position + Vector3.up * alturaNumero, "-" + n, new Color(1f, 0.35f, 0.3f));
        };
        _vida.AlCurar += n => NumeroFlotante.Crear(transform.position + Vector3.up * alturaNumero, "+" + n, new Color(0.4f, 1f, 0.45f));
    }

    void Update()
    {
        if (_rojo && Time.time >= _hasta) Rojo(false);
    }

    /// Cambia todos los materiales por uno rojo liso (multiplicar el color oscurecía la textura).
    void Rojo(bool si)
    {
        _rojo = si;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (!r) continue;
            if (si)
            {
                var rojos = new Material[_originales[i].Length];
                for (int k = 0; k < rojos.Length; k++) rojos[k] = _materialRojo;
                r.sharedMaterials = rojos;
            }
            else r.sharedMaterials = _originales[i];
        }
    }
}
