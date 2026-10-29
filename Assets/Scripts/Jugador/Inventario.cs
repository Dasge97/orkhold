using System;
using UnityEngine;

/// Acceso del jugador al inventario de la partida (Partida), que sobrevive a los cambios de escena.
public class Inventario : MonoBehaviour
{
    public event Action AlCambiar;

    void OnEnable() { Partida.AlCambiar += Avisar; }
    void OnDisable() { Partida.AlCambiar -= Avisar; }
    void Avisar() => AlCambiar?.Invoke();

    public int this[Recurso r] => Partida.Inventario[r];
    public int Bajas => Partida.Actual.bajas;

    public void Sumar(Recurso r, int cantidad = 1) => Partida.Sumar(r, cantidad);
    public bool Gastar(Recurso r, int cantidad = 1) => Partida.Gastar(r, cantidad);
    public void SumarBaja() { Partida.Actual.bajas++; Partida.Cambiado(); }
    public bool UsarPocion() => Partida.Gastar(Recurso.Pocion);
}
