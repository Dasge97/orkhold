using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Ambiente de las mazmorras al montar las escenas (15-interfaz.md §10.3). Sobre el mapa ya cargado:
/// - quita los bloques de muro que no tocan el suelo: oscuridad en vez de un mar de almenas;
/// - pone antorchas en las paredes con fuego, luz y un charco de luz en el suelo;
/// - arrima a las paredes ataúdes, lápidas, urnas y velas (catacumbas) o barriles, armas y andamios (forja);
/// - siembra escombros por el suelo y pone el polvo que flota.
/// Todo con azar fijo por mapa: cada montaje sale igual.
public static class DecorarMazmorra
{
    const string Dungeon = CrearPrefabs.Dungeon;
    const string Cementerio = CrearPrefabs.Cementerio;
    const string RutaResplandor = "Assets/Prefabs/Materiales/Resplandor.mat";

    class Tema
    {
        public string[] pared, suelo, centro;
        public Color tinteAntorcha = Color.white;
        public float verdes;          // parte de las antorchas con fuego verde
        public float densidadPared = 0.7f, densidadSuelo = 0.14f, densidadCentro = 0.06f;
    }

    static readonly Dictionary<string, Tema> Temas = new Dictionary<string, Tema>
    {
        ["catacumbas"] = new Tema
        {
            pared = new[] { "G:coffin", "G:coffin-old", "G:gravestone-cross", "G:gravestone-round", "G:gravestone-broken", "G:gravestone-bevel",
                            "G:urn-round", "G:urn-square", "G:candle-multiple", "G:cross-column", "G:pillar-small", "G:altar-stone", "G:coffin" },
            suelo = new[] { "G:debris", "G:gravestone-debris", "D:stones", "D:rocks", "G:candle", "G:detail-bowl" },
            centro = new[] { "G:crypt-small", "G:pillar-large", "G:coffin-old", "G:altar-stone", "G:grave-border", "G:pillar-obelisk" },
            verdes = 0.35f,
        },
        ["forja"] = new Tema
        {
            pared = new[] { "D:barrel", "D:barrel", "D:wood-structure", "D:wood-support", "D:shield-rectangle", "D:shield-round",
                            "D:weapon-sword", "D:weapon-spear", "D:pot", "D:rocks", "G:debris-wood", "D:banner" },
            suelo = new[] { "D:stones", "D:rocks", "G:debris-wood", "D:dirt", "G:debris" },
            centro = new[] { "D:wood-structure", "D:barrel", "D:table", "G:rocks-tall", "D:wood-support" },
            tinteAntorcha = new Color(1f, 0.85f, 0.75f),
            densidadPared = 0.6f,
        },
    };

    static readonly Vector2Int[] Lados = { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };

    public static void Aplicar(CargadorMapa.Resultado r)
    {
        var mapa = r.mapa;
        if (!Temas.TryGetValue(mapa.Nombre, out var tema)) return;
        var azar = new System.Random(mapa.Nombre.Aggregate(17, (h, ch) => h * 31 + ch) & 0x7fffffff);
        var raiz = r.raiz.transform;
        var deco = new GameObject("Decoracion").transform;
        deco.SetParent(raiz, false);

        QuitarMurosOcultos(r);

        // Casillas que no se tocan: marcas (puertas, enemigos, monedas...) y su alrededor.
        var ocupadas = new HashSet<Vector2Int>();
        var puertas = new HashSet<Vector2Int>();
        foreach (var m in r.marcas)
        {
            // Puertas y arqueros (que huyen hacia atrás), dos casillas libres; el resto de marcas, una.
            int radio = m.entidad && (m.entidad.GetComponent<Puerta>() || m.entidad.GetComponent<EnemigoArquero>()) ? 2 : 1;
            for (int dx = -radio; dx <= radio; dx++)
                for (int dz = -radio; dz <= radio; dz++)
                {
                    ocupadas.Add(new Vector2Int(m.columna + dx, m.fila + dz));
                    if (m.entidad && m.entidad.GetComponent<Puerta>()) puertas.Add(new Vector2Int(m.columna + dx, m.fila + dz));
                }
        }

        int antorchas = 0;
        for (int f = 0; f < mapa.Alto; f++)
            for (int c = 0; c < mapa.Ancho; c++)
            {
                if (mapa.Letra(c, f) != '.') continue;
                double dado = azar.NextDouble(), dado2 = azar.NextDouble();
                var pos = CargadorMapa.Casilla(mapa, c, f);
                // Lado con muro, si es uno solo y enfrente hay sitio (en los pasillos no se pone nada).
                var muros = Lados.Where(d => EsMuro(mapa, c + d.x, f + d.y)).ToArray();
                bool libre = !ocupadas.Contains(new Vector2Int(c, f));
                if (muros.Length == 1 && !EsMuro(mapa, c - muros[0].x, f - muros[0].y) && !puertas.Contains(new Vector2Int(c, f)))
                {
                    // Hacia el muro en el mundo: las filas crecen hacia el sur (z baja).
                    var haciaMuro = new Vector3(muros[0].x, 0f, -muros[0].y);
                    if ((c * 7 + f * 3) % 5 == 0 && dado < 0.75)
                    {
                        Antorcha(deco, pos, haciaMuro, dado2 < tema.verdes ? new Color(0.45f, 1f, 0.55f) : tema.tinteAntorcha);
                        antorchas++;
                    }
                    else if (libre && dado < tema.densidadPared)
                        Pieza(deco, Elegir(tema.pared, azar), pos + haciaMuro * 0.28f, Quaternion.LookRotation(-haciaMuro).eulerAngles.y + (float)(dado2 * 30 - 15), true);
                }
                else if (libre && LejosDeMuros(mapa, c, f, 2) && dado < tema.densidadCentro)
                    Pieza(deco, Elegir(tema.centro, azar), pos, azar.Next(4) * 90f, true);
                else if (libre && muros.Length == 0 && dado < tema.densidadSuelo)
                    Pieza(deco, Elegir(tema.suelo, azar), pos + new Vector3((float)dado2 - 0.5f, 0f, (float)(dado * 5 % 1) - 0.5f) * 0.6f, (float)(dado2 * 360), false);
            }

        var polvo = new GameObject("Polvo");
        polvo.transform.SetParent(raiz, false);
        polvo.AddComponent<Polvo>();
        Debug.Log($"Mazmorra {mapa.Nombre}: {deco.childCount} piezas de decoración, {antorchas} antorchas");
    }

    /// Ninguna casilla a "n" o menos es muro, puerta ni marca: sitio para una pieza grande sin cortar el paso.
    static bool LejosDeMuros(MapaTexto mapa, int c, int f, int n)
    {
        for (int dx = -n; dx <= n; dx++)
            for (int dz = -n; dz <= n; dz++)
            {
                if (EsMuro(mapa, c + dx, f + dz)) return false;
                if (Mathf.Abs(dx) <= 1 && Mathf.Abs(dz) <= 1 && mapa.Letra(c + dx, f + dz) != '.') return false;
            }
        return true;
    }

    static bool EsMuro(MapaTexto mapa, int c, int f) =>
        c < 0 || f < 0 || c >= mapa.Ancho || f >= mapa.Alto || mapa.Letra(c, f) == '#';

    /// Un muro se ve si alguna de las 8 casillas de alrededor no es muro.
    static bool MuroVisible(MapaTexto mapa, int c, int f)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
                if (!EsMuro(mapa, c + dx, f + dz)) return true;
        return false;
    }

    static void QuitarMurosOcultos(CargadorMapa.Resultado r)
    {
        var mapa = r.mapa;
        var ocultas = new HashSet<Vector2Int>();
        for (int f = 0; f < mapa.Alto; f++)
            for (int c = 0; c < mapa.Ancho; c++)
                if (mapa.Letra(c, f) == '#' && !MuroVisible(mapa, c, f))
                {
                    var p = CargadorMapa.Casilla(mapa, c, f);
                    ocultas.Add(new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.z)));
                }
        int quitadas = 0;
        foreach (var nombre in new[] { "Piezas", "Suelo" })
        {
            var cont = r.raiz.transform.Find(nombre);
            if (!cont) continue;
            foreach (var t in cont.Cast<Transform>().ToArray())
            {
                if (t.GetComponents<Component>().Any(k => k is Collider)) continue;   // las colisiones se quedan
                var lp = t.localPosition;
                if (ocultas.Contains(new Vector2Int(Mathf.RoundToInt(lp.x), Mathf.RoundToInt(lp.z))))
                {
                    Object.DestroyImmediate(t.gameObject);
                    quitadas++;
                }
            }
        }
        Debug.Log($"Mazmorra {mapa.Nombre}: {quitadas} piezas de muro y suelo ocultas quitadas");
    }

    static string Elegir(string[] lista, System.Random azar) => lista[azar.Next(lista.Length)];

    static GameObject Pieza(Transform padre, string clave, Vector3 pos, float giro, bool chocar)
    {
        string carpeta = clave.StartsWith("G:") ? Cementerio : Dungeon;
        var g = CrearPrefabs.Poner(carpeta + clave.Substring(2) + ".fbx", padre, pos, giro);
        // Los modelos del Graveyard Kit son más pequeños que los de Mini Dungeon.
        if (clave.StartsWith("G:")) g.transform.localScale = Vector3.one * 1.9f;
        if (chocar)
        {
            var b = Limites(g);
            if (b.size.y > 0.3f)
            {
                var col = g.AddComponent<BoxCollider>();
                col.center = g.transform.InverseTransformPoint(b.center);
                var s = g.transform.lossyScale;
                col.size = new Vector3(b.size.x / s.x, b.size.y / s.y, b.size.z / s.z) * 0.85f;
            }
        }
        return g;
    }

    static Bounds Limites(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// Farol en la pared con fuego pequeño, su luz y un charco de luz en el suelo.
    static void Antorcha(Transform padre, Vector3 pos, Vector3 haciaMuro, Color tinte)
    {
        var g = new GameObject("Antorcha");
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos + haciaMuro * 0.42f;
        var farol = CrearPrefabs.Poner(Cementerio + "lantern-candle.fbx", g.transform, new Vector3(0f, 0.75f, 0f), 0f);
        farol.transform.localScale = Vector3.one * 1.6f;
        var fuego = new GameObject("Fuego");
        fuego.transform.SetParent(g.transform, false);
        fuego.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        var fu = fuego.AddComponent<Fuego>();
        fu.tamano = 0.3f; fu.humo = false; fu.luz = true; fu.alturaLlamas = 0f; fu.tinte = tinte;

        var charco = GameObject.CreatePrimitive(PrimitiveType.Quad);
        charco.name = "Resplandor";
        Object.DestroyImmediate(charco.GetComponent<Collider>());
        charco.transform.SetParent(padre, false);
        charco.transform.localPosition = pos + haciaMuro * 0.15f + Vector3.up * 0.02f;
        charco.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        charco.transform.localScale = Vector3.one * 3.2f;
        var mr = charco.GetComponent<MeshRenderer>();
        mr.sharedMaterial = MaterialResplandor(tinte);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static readonly Dictionary<Color, Material> _resplandores = new Dictionary<Color, Material>();

    /// Material aditivo con un degradado redondo (uno por color de antorcha).
    static Material MaterialResplandor(Color tinte)
    {
        if (_resplandores.TryGetValue(tinte, out var hecho) && hecho) return hecho;
        const string rutaTex = "Assets/Prefabs/Materiales/resplandor.png";
        if (!File.Exists(rutaTex))
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    float a = Mathf.Clamp01(1f - d); a = a * a;
                    t.SetPixel(x, y, new Color(a, a, a, a));
                }
            File.WriteAllBytes(rutaTex, t.EncodeToPNG());
            AssetDatabase.ImportAsset(rutaTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
            imp.wrapMode = TextureWrapMode.Clamp; imp.SaveAndReimport();
        }
        string ruta = tinte == Color.white ? RutaResplandor : RutaResplandor.Replace(".mat", "-" + ColorUtility.ToHtmlStringRGB(tinte) + ".mat");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (!m) { m = new Material(Shader.Find("Legacy Shaders/Particles/Additive")); AssetDatabase.CreateAsset(m, ruta); }
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);
        m.SetColor("_TintColor", new Color(1f, 0.55f, 0.25f, 0.5f) * tinte);
        EditorUtility.SetDirty(m);
        _resplandores[tinte] = m;
        return m;
    }
}
