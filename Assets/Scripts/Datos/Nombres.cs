/// Nombres de los recursos tal como se ven en pantalla.
public static class Nombres
{
    public static string De(Recurso r)
    {
        switch (r)
        {
            case Recurso.Moneda: return "moneda";
            case Recurso.Madera: return "madera";
            case Recurso.Piedra: return "piedra";
            case Recurso.Hierro: return "hierro";
            case Recurso.Hueso: return "hueso";
            default: return "poción";
        }
    }

    public static string Plural(Recurso r, int n)
    {
        if (n == 1) return De(r);
        switch (r)
        {
            case Recurso.Moneda: return "monedas";
            case Recurso.Hueso: return "huesos";
            case Recurso.Pocion: return "pociones";
            default: return De(r);
        }
    }

    /// Nombre del recurso en la API y en el balance (moneda, madera, piedra, hierro, hueso).
    public static string Clave(Recurso r) => r == Recurso.Pocion ? "pocion" : De(r);

    /// Recurso a partir de su nombre en la API; null si no es un recurso.
    public static Recurso? DeClave(string clave)
    {
        foreach (Recurso r in System.Enum.GetValues(typeof(Recurso))) if (Clave(r) == clave) return r;
        return null;
    }
}
