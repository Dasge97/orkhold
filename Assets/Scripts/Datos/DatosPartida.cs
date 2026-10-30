using System;
using System.Collections.Generic;

/// Todo lo que se guarda de una partida: inventario, unidades defensoras, fuerte y progreso.
[Serializable]
public class DatosPartida
{
    public const int VersionActual = 1;
    public int version = VersionActual;
    public DatosInventario inventario = new DatosInventario();
    public int bajas;
    /// Puntuación tipo Elo para elegir rival (empieza en 1000).
    public int puntuacion = 1000;
    public List<DatosUnidad> unidades = new List<DatosUnidad>();
    public DisenoFuerte fuerte = new DisenoFuerte();
    /// Progreso como pares clave-valor (ej. "jefe1_muerto" = "1").
    public List<Par> progreso = new List<Par>();

    [Serializable] public class Par { public string clave, valor; }

    public string Progreso(string clave, string porDefecto = "")
    {
        foreach (var p in progreso) if (p.clave == clave) return p.valor;
        return porDefecto;
    }

    public void PonerProgreso(string clave, string valor)
    {
        foreach (var p in progreso) if (p.clave == clave) { p.valor = valor; return; }
        progreso.Add(new Par { clave = clave, valor = valor });
    }
}
