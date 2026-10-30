using System;

/// Lo que guarda un escondite (08-formato-y-pvp.md §3): una cantidad por recurso. Mismo JSON que la API.
[Serializable]
public class Contenido
{
    public int moneda, madera, piedra, hierro, hueso;

    public static readonly Recurso[] Recursos = { Recurso.Moneda, Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso };

    public int this[Recurso r]
    {
        get => r == Recurso.Moneda ? moneda : r == Recurso.Madera ? madera : r == Recurso.Piedra ? piedra : r == Recurso.Hierro ? hierro : r == Recurso.Hueso ? hueso : 0;
        set
        {
            switch (r)
            {
                case Recurso.Moneda: moneda = value; break;
                case Recurso.Madera: madera = value; break;
                case Recurso.Piedra: piedra = value; break;
                case Recurso.Hierro: hierro = value; break;
                case Recurso.Hueso: hueso = value; break;
            }
        }
    }

    public int Unidades => moneda + madera + piedra + hierro + hueso;

    public Contenido Copia() => (Contenido)MemberwiseClone();

    /// "6 monedas, 2 madera" o "vacío".
    public override string ToString()
    {
        var partes = new System.Collections.Generic.List<string>();
        foreach (var r in Recursos) if (this[r] > 0) partes.Add($"{this[r]} {Nombres.Plural(r, this[r])}");
        return partes.Count > 0 ? string.Join(", ", partes) : "vacío";
    }
}
