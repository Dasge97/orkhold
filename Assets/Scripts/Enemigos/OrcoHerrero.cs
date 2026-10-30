using UnityEngine;

/// Orco herrero de la Forja Hundida (11-mazmorras.md §2): lento y con escudo. El primer golpe de cada tres
/// lo para el escudo (no hace daño).
public class OrcoHerrero : Enemigo
{
    [Tooltip("Cada cuántos golpes para uno con el escudo.")]
    public int cadaCuantos = 3;

    int _golpes;

    /// Golpes parados con el escudo (para los tests).
    public int Parados { get; private set; }

    public override void RecibirGolpe(int cantidad, Vector3 desde, ControlJugador quien)
    {
        if (!PuedeRecibirGolpe) return;
        _golpes++;
        if ((_golpes - 1) % cadaCuantos == 0)
        {
            Parados++;
            Sonidos.Tocar("golpe_roca", transform.position);
            NumeroFlotante.Crear(transform.position + Vector3.up * 1.3f, "¡Escudo!", new Color(0.8f, 0.85f, 0.95f), 0.8f);
            _ocupadoHasta = Time.time + 0.2f;
            if (EstadoActual != Estado.Ataca) EstadoActual = Estado.Persigue;
            return;
        }
        base.RecibirGolpe(cantidad, desde, quien);
    }
}
