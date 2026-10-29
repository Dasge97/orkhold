using System;
using System.Collections.Generic;
using UnityEngine;

/// Lee mapas de texto (Assets/Mapas/*.txt) y coloca sus piezas según una PaletaMapa.
/// Formato: cabecera "clave: valor", una línea "---" y la rejilla (una letra por casilla de 1 m).
/// La casilla (columna 0, última fila) cae en el origen; la fila 0 del fichero es el norte (+Z).
public static class CargadorMapa
{
    /// Casilla que la paleta pide atender fuera del cargador (aparición, hueco del fuerte...).
    public struct Marca
    {
        public char letra;
        public int columna, fila;
        public Vector3 posicion;      // en coordenadas del mundo
        public GameObject entidad;    // el prefab con lógica puesto en la casilla, si lo hay
    }

    /// Resultado de colocar un mapa.
    public class Resultado
    {
        public MapaTexto mapa;
        public GameObject raiz;
        public readonly List<Marca> marcas = new List<Marca>();
        public readonly List<GameObject> entidades = new List<GameObject>();
        /// Piezas que no se pueden fundir con las demás en una sola malla (árboles, rocas).
        public readonly HashSet<GameObject> sueltas = new HashSet<GameObject>();
        public readonly List<char> letrasDesconocidas = new List<char>();
        public int modelos;
        public int colisionesUnidas;

        /// Primera marca con esa letra, o null.
        public Marca? Buscar(char letra)
        {
            foreach (var m in marcas) if (m.letra == letra) return m;
            return null;
        }
    }

    /// Separa cabecera y rejilla. Ignora líneas vacías y espacios al final. Sin "---", todo es rejilla.
    public static MapaTexto Leer(string texto)
    {
        var mapa = new MapaTexto();
        var lineas = texto.Replace("\r", "").Split('\n');
        bool hayCabecera = Array.Exists(lineas, l => l.Trim() == "---");
        bool enRejilla = !hayCabecera;
        var rejilla = new List<string>();
        foreach (var bruta in lineas)
        {
            var l = bruta.TrimEnd();
            if (!enRejilla)
            {
                if (l.Trim() == "---") { enRejilla = true; continue; }
                int dos = l.IndexOf(':');
                if (dos > 0) mapa.cabecera[l.Substring(0, dos).Trim().ToLowerInvariant()] = l.Substring(dos + 1).Trim();
                continue;
            }
            if (l.Length > 0) rejilla.Add(l);
        }
        mapa.PonerFilas(rejilla);
        return mapa;
    }

    /// Centro de una casilla en coordenadas locales del mapa.
    public static Vector3 Casilla(MapaTexto mapa, int columna, int fila)
    {
        return new Vector3(columna, 0f, mapa.Alto - 1 - fila);
    }

    /// Coloca el mapa bajo un objeto nuevo hijo de "padre". "instanciar" permite al editor usar
    /// PrefabUtility.InstantiatePrefab; si es null se usa Object.Instantiate.
    public static Resultado Colocar(MapaTexto mapa, PaletaMapa paleta, Transform padre = null,
                                    Func<GameObject, GameObject> instanciar = null)
    {
        instanciar ??= m => UnityEngine.Object.Instantiate(m);
        var r = new Resultado { mapa = mapa, raiz = new GameObject("Mapa " + mapa.Nombre) };
        if (padre) r.raiz.transform.SetParent(padre, false);
        var suelo = Hijo(r.raiz, "Suelo");
        var piezas = Hijo(r.raiz, "Piezas");
        var entidades = Hijo(r.raiz, "Entidades");

        // Azar fijo por mapa: el cliente y el servidor de mundo eligen las mismas variantes.
        var azar = new System.Random(Semilla(mapa.Nombre));
        var sueloModelos = mapa.Valor("tipo") == "exterior" ? paleta.sueloExterior : paleta.sueloInterior;
        bool flores = mapa.Valor("relleno") == "flores";
        var hecho = new bool[mapa.Ancho, mapa.Alto];

        for (int f = 0; f < mapa.Alto; f++)
            for (int c = 0; c < mapa.Ancho; c++)
            {
                char letra = mapa.Letra(c, f);
                bool vacia = letra == '.' || letra == ' ';
                var e = vacia ? null : paleta.Buscar(letra);
                if (e == null && !vacia && !r.letrasDesconocidas.Contains(letra))
                {
                    r.letrasDesconocidas.Add(letra);
                    Debug.LogWarning($"Mapa {mapa.Nombre}: la letra '{letra}' no está en la paleta (columna {c}, fila {f}).");
                }

                var pos = Casilla(mapa, c, f);
                if (e == null || e.conSuelo)
                    Poner(Elegir(sueloModelos, azar), suelo.transform, pos, azar.Next(4) * 90f, Vector3.one, instanciar, r);

                // Relleno decorativo: siempre se tira el dado para que el azar no dependa del relleno.
                double dado = azar.NextDouble();
                if (vacia && flores && dado < paleta.densidadFlores)
                {
                    var p = pos + new Vector3((float)azar.NextDouble() * 0.6f - 0.3f, 0f, (float)azar.NextDouble() * 0.6f - 0.3f);
                    Poner(Elegir(paleta.flores, azar), piezas.transform, p, (float)azar.NextDouble() * 360f, Vector3.one, instanciar, r);
                }

                if (e == null || hecho[c, f]) continue;

                int n = Mathf.Max(1, e.bloque);
                if (n > 1)
                {
                    for (int dz = 0; dz < n; dz++)
                        for (int dx = 0; dx < n; dx++)
                            if (c + dx < mapa.Ancho && f + dz < mapa.Alto && mapa.Letra(c + dx, f + dz) == letra)
                                hecho[c + dx, f + dz] = true;
                    pos += new Vector3((n - 1) / 2f, 0f, -(n - 1) / 2f);
                }

                var modelo = Elegir(e.modelos, azar);
                float giro = e.giroAleatorio ? (float)azar.NextDouble() * 360f : e.giroY;
                GameObject g = null;

                if (e.union.brazo)
                    g = PonerBrazos(e, Mascara(mapa, c, f, e), piezas.transform, pos, instanciar, r);
                else if (e.union.Activa)
                    modelo = PiezaUnion(e.union, Mascara(mapa, c, f, e), out giro);

                // Lo que va debajo (río bajo el puente) y el modelo propio girado para cruzarlo.
                var abajo = e.debajo != '\0' ? paleta.Buscar(e.debajo) : null;
                if (abajo != null && abajo.union.Activa)
                {
                    int m = Mascara(mapa, c, f, abajo);
                    Poner(PiezaUnion(abajo.union, m, out float giroAbajo), suelo.transform, pos, giroAbajo, Vector3.one, instanciar, r);
                    giro = (m & (Norte | Sur)) != 0 ? 0f : 90f;
                }

                if (!g) g = Poner(modelo, piezas.transform, pos + Vector3.up * e.altura, giro, Vector3.one * e.escala, instanciar, r);
                if (!e.unirColision && e.colision != PaletaMapa.Colision.Ninguna)
                {
                    if (!g)
                    {
                        g = new GameObject(e.descripcion);
                        g.transform.SetParent(piezas.transform, false);
                        g.transform.localPosition = pos;
                        g.transform.localScale = Vector3.one * e.escala;
                    }
                    PonerColision(g, e, g.transform.localScale.x);
                }
                int aBorde = Mathf.Min(Mathf.Min(c, f), Mathf.Min(mapa.Ancho - 1 - c, mapa.Alto - 1 - f));
                if (g && !e.combinar && aBorde >= e.combinarEnBorde)
                {
                    r.sueltas.Add(g);
                    if (e.esFuente) g.AddComponent<FuenteRecurso>().recurso = e.recursoFuente;
                }

                GameObject ent = null;
                if (e.entidad)
                {
                    ent = instanciar(e.entidad);
                    ent.transform.SetParent(entidades.transform, false);
                    ent.transform.localPosition = pos;
                    ent.transform.localRotation = Quaternion.identity;
                    r.entidades.Add(ent);
                }

                if (e.marca || ent)
                    r.marcas.Add(new Marca { letra = letra, columna = c, fila = f, posicion = r.raiz.transform.TransformPoint(pos), entidad = ent });
            }

        UnirColisiones(mapa, paleta, r);

        // Un único collider plano para todo el suelo.
        var col = suelo.AddComponent<BoxCollider>();
        col.center = new Vector3((mapa.Ancho - 1) / 2f, -0.25f, (mapa.Alto - 1) / 2f);
        col.size = new Vector3(mapa.Ancho, 0.5f, mapa.Alto);
        return r;
    }

    /// Atajo: lee y coloca.
    public static Resultado Cargar(string texto, PaletaMapa paleta, Transform padre = null,
                                   Func<GameObject, GameObject> instanciar = null)
    {
        return Colocar(Leer(texto), paleta, padre, instanciar);
    }

    /// ¿Se puede andar por esta casilla? Vacía, o letra sin colisión.
    public static bool Transitable(MapaTexto mapa, PaletaMapa paleta, int c, int f)
    {
        if (c < 0 || f < 0 || c >= mapa.Ancho || f >= mapa.Alto) return false;
        char l = mapa.Letra(c, f);
        if (l == '.' || l == ' ') return true;
        var e = paleta.Buscar(l);
        return e == null || e.colision == PaletaMapa.Colision.Ninguna;
    }

    // ---------- colisiones unidas ----------

    /// Una caja por cada tramo de casillas seguidas de la misma letra en una fila (tarea 1.7).
    static void UnirColisiones(MapaTexto mapa, PaletaMapa paleta, Resultado r)
    {
        GameObject contenedor = null;
        for (int f = 0; f < mapa.Alto; f++)
        {
            int c = 0;
            while (c < mapa.Ancho)
            {
                char l = mapa.Letra(c, f);
                var e = l == '.' ? null : paleta.Buscar(l);
                if (e == null || !e.unirColision || e.colision == PaletaMapa.Colision.Ninguna) { c++; continue; }
                int inicio = c;
                while (c < mapa.Ancho && mapa.Letra(c, f) == l) c++;
                int largo = c - inicio;
                if (!contenedor) contenedor = Hijo(r.raiz, "Colisiones");
                var g = new GameObject($"{e.descripcion} fila {f} ({largo})");
                g.transform.SetParent(contenedor.transform, false);
                var a = Casilla(mapa, inicio, f);
                g.transform.localPosition = a + new Vector3((largo - 1) / 2f, 0f, 0f);
                var b = g.AddComponent<BoxCollider>();
                b.size = new Vector3(largo - 1 + e.tamano.x, e.tamano.y, e.tamano.z);
                b.center = new Vector3(0f, e.tamano.y / 2f, 0f);
                r.colisionesUnidas++;
            }
        }
    }

    // ---------- piezas de unión ----------

    public const int Norte = 1, Este = 2, Sur = 4, Oeste = 8;

    /// Lados de la casilla con un vecino unido. Fuera del mapa cuenta como unido
    /// (un río que sale por el borde sigue abierto), salvo para tramos (vallas).
    public static int Mascara(MapaTexto mapa, int c, int f, PaletaMapa.Entrada e)
    {
        bool bordeUne = !e.union.brazo;
        bool Unida(int cc, int ff)
        {
            if (cc < 0 || ff < 0 || cc >= mapa.Ancho || ff >= mapa.Alto) return bordeUne;
            char l = mapa.Letra(cc, ff);
            return l == e.letra || e.union.unidaCon.IndexOf(l) >= 0;
        }
        int m = 0;
        if (Unida(c, f - 1)) m |= Norte;
        if (Unida(c + 1, f)) m |= Este;
        if (Unida(c, f + 1)) m |= Sur;
        if (Unida(c - 1, f)) m |= Oeste;
        return m;
    }

    /// Gira una máscara 90° en sentido horario visto desde arriba (norte pasa a este).
    static int Girar(int m) => ((m << 1) | (m >> 3)) & 15;

    /// Elige la pieza cuya forma, girada 0/90/180/270, coincide con la máscara.
    public static GameObject PiezaUnion(PaletaMapa.PiezasUnion u, int mascara, out float giro)
    {
        var formas = new (GameObject modelo, int lados)[]
        {
            (u.cruce, 15), (u.te, Oeste | Este | Sur), (u.curva, Sur | Este),
            (u.recto, Norte | Sur), (u.final, Sur), (u.aislado, 0),
        };
        foreach (var (modelo, lados) in formas)
        {
            if (!modelo) continue;
            int l = lados;
            for (int vueltas = 0; vueltas < 4; vueltas++, l = Girar(l))
                if (l == mascara) { giro = vueltas * 90f; return modelo; }
        }
        // Forma sin pieza propia: recto en la dirección que más se parezca.
        giro = (mascara & (Norte | Sur)) != 0 ? 0f : 90f;
        return u.recto;
    }

    /// Un tramo desde el centro hacia cada lado unido. Sin vecinos, un tramo este-oeste entero.
    static GameObject PonerBrazos(PaletaMapa.Entrada e, int mascara, Transform padre, Vector3 pos,
                                  Func<GameObject, GameObject> instanciar, Resultado r)
    {
        var raiz = new GameObject(e.descripcion);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = pos;
        if (mascara == 0) mascara = Este | Oeste;
        for (int lado = 0; lado < 4; lado++)
        {
            if ((mascara & (1 << lado)) == 0) continue;
            var hueco = new GameObject("tramo");
            hueco.transform.SetParent(raiz.transform, false);
            hueco.transform.localRotation = Quaternion.Euler(0f, lado * 90f, 0f);
            var t = instanciar(e.union.brazo);
            t.transform.SetParent(hueco.transform, false);
            t.transform.localPosition = e.union.brazoPosicion;
            t.transform.localRotation = Quaternion.Euler(e.union.brazoGiro);
            t.transform.localScale = e.union.brazoEscala;
            r.modelos++;
        }
        return raiz;
    }

    // ---------- ayudas ----------

    static GameObject Hijo(GameObject padre, string nombre)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre.transform, false);
        return g;
    }

    static GameObject Elegir(GameObject[] modelos, System.Random azar)
    {
        if (modelos == null || modelos.Length == 0) return null;
        return modelos[azar.Next(modelos.Length)];
    }

    static GameObject Poner(GameObject modelo, Transform padre, Vector3 pos, float giroY, Vector3 escala,
                            Func<GameObject, GameObject> instanciar, Resultado r)
    {
        if (!modelo) return null;
        var g = instanciar(modelo);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        g.transform.localScale = escala;
        r.modelos++;
        return g;
    }

    /// El tamaño de la paleta va en metros del mundo; se divide por la escala del objeto.
    static void PonerColision(GameObject g, PaletaMapa.Entrada e, float escalaObjeto)
    {
        float k = escalaObjeto > 0f ? 1f / escalaObjeto : 1f;
        switch (e.colision)
        {
            case PaletaMapa.Colision.Caja:
                var b = g.AddComponent<BoxCollider>();
                b.size = e.tamano * k;
                b.center = new Vector3(0f, e.tamano.y / 2f * k, 0f);
                break;
            case PaletaMapa.Colision.Capsula:
                var cap = g.AddComponent<CapsuleCollider>();
                cap.radius = e.tamano.x * k;
                cap.height = e.tamano.y * k;
                cap.center = new Vector3(0f, e.tamano.y / 2f * k, 0f);
                break;
        }
    }

    /// Hash estable (string.GetHashCode puede cambiar entre plataformas).
    static int Semilla(string s)
    {
        unchecked
        {
            int h = 17;
            foreach (char ch in s) h = h * 31 + ch;
            return h;
        }
    }
}
