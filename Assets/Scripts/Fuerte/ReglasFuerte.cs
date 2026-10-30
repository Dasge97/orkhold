using UnityEngine;

/// Reglas de construcción del fuerte. La API (fase 6) aplica las mismas al guardar.
/// - Solo en casillas vacías ('.') de fuerte-base.txt, y nunca en la casilla de llegada junto a la entrada.
/// - Límites por categoría: 40 muros (incluidas puertas), 6 trampas, 4 defensores.
/// - Coste en recursos; el orco jefe gasta una unidad del inventario.
/// - Mejora de defensores: +1 vida por 2 huesos, hasta +3.
public static class ReglasFuerte
{
    public static Vector3 Celda(int x, int z) => new Vector3(x, 0f, z);

    /// Columna y fila del fichero a partir de x y z del diseño.
    public static char Letra(MapaTexto mapa, int x, int z) => mapa.Letra(x, mapa.Alto - 1 - z);

    /// La casilla junto a la entrada 'E' hacia dentro: ahí aparece el atacante y no se construye.
    public static Vector2Int Llegada(MapaTexto mapa)
    {
        for (int f = 0; f < mapa.Alto; f++)
            for (int c = 0; c < mapa.Ancho; c++)
                if (mapa.Letra(c, f) == 'E')
                {
                    foreach (var d in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                    {
                        int cc = c + d.x, ff = f + d.y;
                        if (mapa.Letra(cc, ff) == '.') return new Vector2Int(cc, mapa.Alto - 1 - ff);
                    }
                }
        return new Vector2Int(-1, -1);
    }

    /// Posición (x, z) del cofre del tesoro 'K'.
    public static Vector2Int Cofre(MapaTexto mapa)
    {
        for (int f = 0; f < mapa.Alto; f++)
            for (int c = 0; c < mapa.Ancho; c++)
                if (mapa.Letra(c, f) == 'K') return new Vector2Int(c, mapa.Alto - 1 - f);
        return new Vector2Int(-1, -1);
    }

    public static bool CasillaConstruible(MapaTexto mapa, int x, int z)
    {
        if (x < 0 || z < 0 || x >= mapa.Ancho || z >= mapa.Alto) return false;
        if (Letra(mapa, x, z) != '.') return false;
        return Llegada(mapa) != new Vector2Int(x, z);
    }

    /// ¿Se puede poner la pieza en (x, z) con este inventario? Si no, "motivo" lo explica.
    public static bool PuedePoner(DisenoFuerte d, CatalogoPiezas cat, CatalogoPiezas.Pieza p, MapaTexto mapa,
                                  DatosPartida partida, int x, int z, out string motivo)
    {
        motivo = "";
        if (p == null) { motivo = "Pieza desconocida"; return false; }
        if (!CasillaConstruible(mapa, x, z)) { motivo = "Ahí no se puede construir"; return false; }
        if (d.En(x, z) != null) { motivo = "Casilla ocupada"; return false; }
        if (p.categoria == CatalogoPiezas.Categoria.Escondite)
        {
            // Cada tipo de escondite tiene su límite (08 §3.2); 0 = todavía bloqueado.
            if (p.limite <= 0) { motivo = $"{p.nombre}: todavía no se puede construir"; return false; }
            if (d.Contar(q => q.tipo == p.id) >= p.limite) { motivo = $"Límite alcanzado: {p.limite} {p.nombre.ToLowerInvariant()}"; return false; }
        }
        else
        {
            int usadas = d.Contar(q => cat.Buscar(q.tipo)?.categoria == p.categoria);
            if (usadas >= cat.Limite(p.categoria))
            {
                motivo = $"Límite alcanzado: {cat.Limite(p.categoria)} {NombreCategoria(p.categoria)}";
                return false;
            }
        }
        if (!string.IsNullOrEmpty(p.requiereUnidad))
        {
            if (!partida.unidades.Exists(u => u.tipo == p.requiereUnidad)) { motivo = "No tienes esa unidad"; return false; }
            return true;
        }
        foreach (var c in p.costes)
            if (partida.inventario[c.recurso] < c.cantidad)
            {
                motivo = $"Te falta {Nombres.De(c.recurso)} ({partida.inventario[c.recurso]}/{c.cantidad})";
                return false;
            }
        return true;
    }

    /// Pone la pieza y paga su coste. Devuelve falso (sin tocar nada) si no se puede.
    public static bool Poner(DisenoFuerte d, CatalogoPiezas cat, string tipo, MapaTexto mapa, DatosPartida partida,
                             int x, int z, out string motivo)
    {
        var p = cat.Buscar(tipo);
        if (!PuedePoner(d, cat, p, mapa, partida, x, z, out motivo)) return false;
        int vidaExtra = 0;
        if (!string.IsNullOrEmpty(p.requiereUnidad))
        {
            var u = partida.unidades.Find(q => q.tipo == p.requiereUnidad);
            partida.unidades.Remove(u);
        }
        else foreach (var c in p.costes) partida.inventario[c.recurso] -= c.cantidad;
        d.piezas.Add(new DisenoFuerte.PiezaDiseno { x = x, z = z, tipo = tipo, vida_extra = vidaExtra });
        return true;
    }

    /// Quita la pieza de (x, z) y devuelve todo lo que costó, mejoras incluidas (igual que la API:
    /// el fuerte cuesta la suma de sus piezas y mejoras, y al guardar se paga o devuelve la diferencia).
    public static bool Quitar(DisenoFuerte d, CatalogoPiezas cat, DatosPartida partida, int x, int z)
    {
        var q = d.En(x, z);
        if (q == null) return false;
        var p = cat.Buscar(q.tipo);
        d.piezas.Remove(q);
        if (p == null) return true;
        if (!string.IsNullOrEmpty(p.requiereUnidad)) partida.unidades.Add(new DatosUnidad { tipo = p.requiereUnidad });
        else foreach (var c in p.costes) partida.inventario[c.recurso] += c.cantidad;
        partida.inventario.hueso += q.vida_extra * cat.huesosPorMejora;
        return true;
    }

    /// Mejora un defensor: +1 vida por "huesosPorMejora" huesos, hasta "mejoraMaxima".
    public static bool Mejorar(DisenoFuerte d, CatalogoPiezas cat, DatosPartida partida, int x, int z, out string motivo)
    {
        motivo = "";
        var q = d.En(x, z);
        var p = q != null ? cat.Buscar(q.tipo) : null;
        if (p == null || p.categoria != CatalogoPiezas.Categoria.Defensor) { motivo = "Solo se mejoran defensores"; return false; }
        if (q.vida_extra >= cat.mejoraMaxima) { motivo = $"Ya tiene la mejora máxima (+{cat.mejoraMaxima})"; return false; }
        if (partida.inventario.hueso < cat.huesosPorMejora) { motivo = $"Hacen falta {cat.huesosPorMejora} huesos"; return false; }
        partida.inventario.hueso -= cat.huesosPorMejora;
        q.vida_extra++;
        return true;
    }

    public static string NombreCategoria(CatalogoPiezas.Categoria c) =>
        c == CatalogoPiezas.Categoria.Muro ? "muros" : c == CatalogoPiezas.Categoria.Trampa ? "trampas" :
        c == CatalogoPiezas.Categoria.Escondite ? "escondites" : "defensores";
}
