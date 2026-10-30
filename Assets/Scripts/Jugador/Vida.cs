using System;
using UnityEngine;

/// Puntos de vida de un personaje (jugador, enemigo) o de una pieza que se rompe.
public class Vida : MonoBehaviour
{
    public int maxima = 3;
    public int actual;
    [Tooltip("No pierde vida (modo prueba).")]
    public bool invulnerable;

    public bool Muerto => actual <= 0;
    /// Momento (Time.time) del último daño recibido.
    public float UltimoDano { get; private set; } = -999f;

    public event Action AlMorir;
    public event Action AlCambiar;
    /// Cantidad y punto desde el que llega el golpe.
    public event Action<int, Vector3> AlRecibirDano;
    public event Action<int> AlCurar;

    void Awake() { actual = maxima; }

    public void Danar(int cantidad) => Danar(cantidad, transform.position);

    public void Danar(int cantidad, Vector3 desde)
    {
        if (Muerto || cantidad <= 0) return;
        UltimoDano = Time.time;
        if (!invulnerable) actual = Mathf.Max(0, actual - cantidad);
        AlRecibirDano?.Invoke(cantidad, desde);
        AlCambiar?.Invoke();
        if (Muerto) AlMorir?.Invoke();
    }

    public void Curar(int cantidad)
    {
        if (Muerto) return;
        int antes = actual;
        actual = Mathf.Min(maxima, actual + cantidad);
        if (actual == antes) return;
        AlCurar?.Invoke(actual - antes);
        AlCambiar?.Invoke();
    }

    public void CurarDelTodo() => Curar(maxima);

    /// Pone la vida que dice el servidor (copias en los clientes), con los mismos avisos que un daño o una cura.
    public void Establecer(int nueva, Vector3 desde)
    {
        nueva = Mathf.Clamp(nueva, 0, maxima);
        if (nueva == actual) return;
        bool estabaMuerto = Muerto;
        int antes = actual;
        actual = nueva;
        if (nueva < antes) { UltimoDano = Time.time; AlRecibirDano?.Invoke(antes - nueva, desde); }
        else if (!estabaMuerto) AlCurar?.Invoke(nueva - antes);
        AlCambiar?.Invoke();
        if (Muerto && !estabaMuerto) AlMorir?.Invoke();
    }

    /// Vuelve a la vida con la vida al máximo.
    public void Revivir()
    {
        actual = maxima;
        AlCambiar?.Invoke();
    }
}
