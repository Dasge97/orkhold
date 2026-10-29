using UnityEngine;

/// Lo que recibe golpes del botón de atacar: enemigos, árboles, rocas y piezas del fuerte.
public interface IGolpeable
{
    bool PuedeRecibirGolpe { get; }
    Transform Transform { get; }
    void RecibirGolpe(int dano, Vector3 desde, ControlJugador quien);
}
