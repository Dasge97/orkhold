using System;
using UnityEngine;

public class Vida : MonoBehaviour
{
    public int maxima = 3;
    public int actual;
    public bool Muerto => actual <= 0;
    public event Action AlMorir;
    public event Action AlCambiar;

    void Awake() { actual = maxima; }

    public void Danar(int cantidad)
    {
        if (Muerto) return;
        actual = Mathf.Max(0, actual - cantidad);
        AlCambiar?.Invoke();
        if (Muerto) AlMorir?.Invoke();
    }

    public void Curar(int cantidad)
    {
        if (Muerto) return;
        actual = Mathf.Min(maxima, actual + cantidad);
        AlCambiar?.Invoke();
    }
}
