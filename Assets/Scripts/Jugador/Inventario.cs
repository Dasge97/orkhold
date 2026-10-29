using System;
using UnityEngine;

/// Lo que el jugador recolecta en el PvE. Es lo que más adelante se gastará en el PvP.
public class Inventario : MonoBehaviour
{
    public int monedas;
    public int pociones;
    public int bajas;        // enemigos matados
    public event Action AlCambiar;

    public void Sumar(Recogible.Clase clase, int cantidad = 1)
    {
        if (clase == Recogible.Clase.Moneda) monedas += cantidad;
        else pociones += cantidad;
        AlCambiar?.Invoke();
    }

    public void SumarBaja() { bajas++; AlCambiar?.Invoke(); }

    public bool UsarPocion()
    {
        if (pociones <= 0) return false;
        pociones--; AlCambiar?.Invoke(); return true;
    }
}
