using UnityEngine;

/// Motas de polvo que flotan despacio alrededor del jugador en las mazmorras (15-interfaz.md §10.3).
/// Un solo sistema de partículas que sigue a la cámara; se crea al arrancar con el material de Resources/ParticulaSuma.
public class Polvo : MonoBehaviour
{
    public Color color = new Color(1f, 0.85f, 0.6f, 0.35f);

    void Start()
    {
        var ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.loop = true; m.duration = 5f;
        m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        m.startSpeed = 0f;
        m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        m.startColor = color;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = 120;
        m.prewarm = true;
        var e = ps.emission; e.rateOverTime = 18f;
        var s = ps.shape; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(14f, 2.5f, 12f);
        var n = ps.noise; n.enabled = true; n.strength = 0.15f; n.frequency = 0.25f; n.scrollSpeed = 0.1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Resources.Load<Material>("ParticulaSuma");
        ps.Play();
    }

    void LateUpdate()
    {
        var j = ControlJugador.Local;
        if (j) transform.position = j.transform.position + Vector3.up * 1.3f;
    }
}
