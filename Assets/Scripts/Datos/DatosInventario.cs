using System;
using UnityEngine;

/// Cantidades de cada recurso. Serializable con JsonUtility (un campo por recurso).
[Serializable]
public class DatosInventario
{
    public int monedas, madera, piedra, hierro, hueso, pociones;

    public int this[Recurso r]
    {
        get
        {
            switch (r)
            {
                case Recurso.Moneda: return monedas;
                case Recurso.Madera: return madera;
                case Recurso.Piedra: return piedra;
                case Recurso.Hierro: return hierro;
                case Recurso.Hueso: return hueso;
                default: return pociones;
            }
        }
        set
        {
            value = Mathf.Max(0, value);
            switch (r)
            {
                case Recurso.Moneda: monedas = value; break;
                case Recurso.Madera: madera = value; break;
                case Recurso.Piedra: piedra = value; break;
                case Recurso.Hierro: hierro = value; break;
                case Recurso.Hueso: hueso = value; break;
                default: pociones = value; break;
            }
        }
    }

    public DatosInventario Copia() => (DatosInventario)MemberwiseClone();

    public static readonly Recurso[] Todos = (Recurso[])Enum.GetValues(typeof(Recurso));
}
