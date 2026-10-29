using System.Collections.Generic;

/// Un mapa leído de texto: la cabecera (clave: valor) y la rejilla de letras.
/// La fila 0 del fichero es el norte. Cada letra es una casilla de 1 m.
public class MapaTexto
{
    public readonly Dictionary<string, string> cabecera = new Dictionary<string, string>();
    public string[] filas = new string[0];

    public int Ancho { get; private set; }
    public int Alto => filas.Length;
    public string Nombre => Valor("nombre", "sin-nombre");

    /// Devuelve el valor de la cabecera o el que se pase por defecto si no está.
    public string Valor(string clave, string porDefecto = "")
    {
        return cabecera.TryGetValue(clave, out var v) ? v : porDefecto;
    }

    /// Letra en la columna y fila del fichero. Fuera de la rejilla devuelve un espacio.
    public char Letra(int columna, int fila)
    {
        if (fila < 0 || fila >= filas.Length) return ' ';
        var f = filas[fila];
        return columna >= 0 && columna < f.Length ? f[columna] : ' ';
    }

    /// Fija las filas y rellena con '.' las que sean más cortas que la más larga.
    public void PonerFilas(List<string> lineas)
    {
        Ancho = 0;
        foreach (var l in lineas) if (l.Length > Ancho) Ancho = l.Length;
        filas = new string[lineas.Count];
        for (int i = 0; i < lineas.Count; i++) filas[i] = lineas[i].PadRight(Ancho, '.');
    }
}
