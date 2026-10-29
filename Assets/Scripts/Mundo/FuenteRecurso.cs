using System.Collections;
using UnityEngine;

/// Árbol o roca que suelta un recurso tras varios golpes (01-diseno.md §4.4).
/// Cada golpe lo hace temblar; al soltar el recurso se queda pequeño ("agotado") 60 s.
public class FuenteRecurso : MonoBehaviour, IGolpeable
{
    public Recurso recurso = Recurso.Madera;
    public int golpesNecesarios = 3;
    public float segundosAgotado = 60f;
    [Tooltip("Tamaño mientras está agotado, respecto al normal.")]
    public float escalaAgotado = 0.35f;

    int _golpes;
    float _agotadoHasta = -1f;
    Vector3 _escala;
    Quaternion _giro;
    Coroutine _temblor;

    public bool Agotado => Time.time < _agotadoHasta;
    public int Golpes => _golpes;
    public bool PuedeRecibirGolpe => !Agotado;
    public Transform Transform => transform;

    void Awake()
    {
        _escala = transform.localScale;
        _giro = transform.localRotation;
    }

    public void RecibirGolpe(int dano, Vector3 desde, ControlJugador quien)
    {
        if (Agotado) return;
        _golpes++;
        Sonidos.Tocar(recurso == Recurso.Madera ? "golpe_madera" : "golpe_roca", transform.position);
        if (_temblor != null) StopCoroutine(_temblor);
        _temblor = StartCoroutine(Temblar(desde));
        if (_golpes < golpesNecesarios) return;

        _golpes = 0;
        if (recurso == Recurso.Madera) Sonidos.Tocar("talar", transform.position);
        Vector3 hacia = desde - transform.position; hacia.y = 0f;
        Vector3 donde = transform.position + (hacia.sqrMagnitude > 0.01f ? hacia.normalized : Vector3.back) * 0.55f + Vector3.up * 0.1f;
        Catalogo.Soltar(recurso, donde);
        StartCoroutine(Agotar());
    }

    IEnumerator Temblar(Vector3 desde)
    {
        Vector3 eje = Vector3.Cross(Vector3.up, transform.position - desde).normalized;
        if (eje.sqrMagnitude < 0.01f) eje = Vector3.right;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float angulo = Mathf.Sin(t * 60f) * 7f * (1f - t / 0.25f);
            transform.localRotation = Quaternion.AngleAxis(angulo, eje) * _giro;
            yield return null;
        }
        transform.localRotation = _giro;
        _temblor = null;
    }

    IEnumerator Agotar()
    {
        _agotadoHasta = Time.time + segundosAgotado;
        yield return Escalar(_escala, _escala * escalaAgotado, 0.3f);
        while (Agotado) yield return null;
        yield return Escalar(transform.localScale, _escala, 0.6f);
    }

    IEnumerator Escalar(Vector3 desde, Vector3 hasta, float segundos)
    {
        float t = 0f;
        while (t < segundos)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(desde, hasta, t / segundos);
            yield return null;
        }
        transform.localScale = hasta;
    }
}
