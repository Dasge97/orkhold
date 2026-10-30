using UnityEngine;

/// Fuego vivo (15-interfaz.md §10): llamas, chispas que suben, humo y una luz cálida que parpadea.
/// Lo llevan la Brasa del pueblo, los braseros de las mazmorras y las chimeneas (solo humo).
/// Las partículas se crean al arrancar, con los materiales de Resources: Particula (humo) y ParticulaSuma (fuego).
public class Fuego : MonoBehaviour
{
    [Tooltip("Tamaño del fuego (1 = brasero; 2,5 = la Brasa).")]
    public float tamano = 1f;
    public bool llamas = true;
    public bool humo = true;
    [Tooltip("Luz que parpadea (cuesta rendimiento: solo en los fuegos grandes o importantes).")]
    public bool luz = true;
    public float alturaLlamas = 0.15f;
    [Tooltip("Color del fuego y de su luz (blanco = naranja normal; verde = velas de las catacumbas).")]
    public Color tinte = Color.white;

    Light _luz;
    float _base, _semilla;

    void Start()
    {
        var mat = Resources.Load<Material>("Particula");
        var suma = Resources.Load<Material>("ParticulaSuma");
        if (llamas)
        {
            Sistema("Llamas", suma, new Color(1f, 0.55f, 0.15f, 0.9f) * tinte, new Color(1f, 0.2f, 0.05f, 0f) * tinte, 0.55f, 0.6f * tamano, 0.35f * tamano,
                    18f * tamano, 0.18f * tamano, 0.9f, additive: true);
            Sistema("Chispas", suma, new Color(1f, 0.8f, 0.3f, 1f) * tinte, new Color(1f, 0.4f, 0.1f, 0f) * tinte, 1.6f, 0.06f * tamano, 0.05f,
                    6f * tamano, 0.25f * tamano, 1.6f, additive: true, ruido: true);
        }
        if (humo)
            Sistema("Humo", mat, new Color(0.25f, 0.24f, 0.24f, 0.35f), new Color(0.3f, 0.3f, 0.3f, 0f), 3.5f, 0.5f * tamano, 1.4f * tamano,
                    4f * Mathf.Max(0.6f, tamano), 0.12f * tamano, 0.5f, additive: false, alto: llamas ? 0.6f * tamano : 0f);
        if (luz)
        {
            var g = new GameObject("Luz");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(0f, 0.6f * tamano, 0f);
            _luz = g.AddComponent<Light>();
            _luz.type = LightType.Point;
            _luz.color = new Color(1f, 0.62f, 0.3f) * tinte;
            _luz.range = 3.5f * tamano;
            _luz.intensity = _base = 1.4f + 0.3f * tamano;
            _luz.shadows = LightShadows.None;
            _semilla = Random.value * 100f;
        }
    }

    void Update()
    {
        if (!_luz) return;
        // Parpadeo suave: dos ondas de ruido.
        float t = Time.time * 7f + _semilla;
        _luz.intensity = _base * (0.85f + 0.15f * Mathf.PerlinNoise(t, 0.3f) + 0.08f * Mathf.Sin(t * 2.3f));
    }

    void Sistema(string nombre, Material mat, Color inicio, Color fin, float vida, float tam0, float tam1, float porSegundo,
                 float radio, float velocidad, bool additive, bool ruido = false, float alto = 0f)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0f, alturaLlamas * tamano + alto, 0f);
        g.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.loop = true; m.duration = 1f; m.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.7f, vida);
        m.startSpeed = new ParticleSystem.MinMaxCurve(velocidad * 0.6f, velocidad);
        m.startSize = new ParticleSystem.MinMaxCurve(tam0 * 0.7f, tam0);
        m.startColor = inicio;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 80;
        var e = ps.emission; e.rateOverTime = porSegundo;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Cone; s.angle = additive ? 8f : 15f; s.radius = radio;
        var col = ps.colorOverLifetime; col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(inicio, 0f), new GradientColorKey(fin, 1f) },
                   new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(inicio.a, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, tam1 / Mathf.Max(0.01f, tam0)));
        if (ruido) { var n = ps.noise; n.enabled = true; n.strength = 0.4f; n.frequency = 0.8f; }
        var r = g.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingFudge = additive ? 0f : 5f;
        ps.Play();
    }
}
