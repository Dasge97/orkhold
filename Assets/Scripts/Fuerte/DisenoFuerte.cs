using System;
using System.Collections.Generic;
using UnityEngine;

/// Diseño del fuerte (02-arquitectura.md §2.5). Se guarda en local y en la API.
/// Coordenadas: x = columna de fuerte-base.txt (0 = oeste); z = fila contada desde el sur (0 = sur).
[Serializable]
public class DisenoFuerte
{
    public const int VersionActual = 1;
    public int version = VersionActual;
    public List<PiezaDiseno> piezas = new List<PiezaDiseno>();

    [Serializable]
    public class PiezaDiseno
    {
        public int x, z;
        public string tipo;
        public int vida_extra;
    }

    public PiezaDiseno En(int x, int z)
    {
        foreach (var p in piezas) if (p.x == x && p.z == z) return p;
        return null;
    }

    public int Contar(Func<PiezaDiseno, bool> condicion)
    {
        int n = 0;
        foreach (var p in piezas) if (condicion(p)) n++;
        return n;
    }

    public DisenoFuerte Copia() => JsonUtility.FromJson<DisenoFuerte>(JsonUtility.ToJson(this));

    public string AJson() => JsonUtility.ToJson(this);

    public static DisenoFuerte DeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new DisenoFuerte();
        var d = JsonUtility.FromJson<DisenoFuerte>(json) ?? new DisenoFuerte();
        d.piezas ??= new List<PiezaDiseno>();
        return d;
    }
}
