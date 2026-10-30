using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Ambiente más sobrio para un valle de orcos (15-interfaz.md, petición de Daniel del 30-10-2026): los colores
/// pastel de los packs de Kenney se cambian, al montar las escenas, por copias más oscuras y menos saturadas.
/// No se tocan los ficheros de Assets/Kenney: las copias viven en Assets/Prefabs/Materiales/Ambiente.
public static class EstiloVisual
{
    const string Carpeta = "Assets/Prefabs/Materiales/Ambiente/";

    /// Colores fijos para los materiales más visibles del Nature Kit (por nombre de material).
    static readonly Dictionary<string, Color> Fijos = new Dictionary<string, Color>
    {
        ["grass"] = new Color(0.36f, 0.45f, 0.25f),
        ["leafsGreen"] = new Color(0.24f, 0.38f, 0.24f),
        ["leafsDark"] = new Color(0.18f, 0.29f, 0.2f),
        ["leafsFall"] = new Color(0.5f, 0.36f, 0.2f),
        ["dirt"] = new Color(0.42f, 0.32f, 0.24f),
        ["dirtDark"] = new Color(0.32f, 0.24f, 0.18f),
        ["woodBark"] = new Color(0.36f, 0.26f, 0.19f),
        ["wood"] = new Color(0.5f, 0.38f, 0.27f),
        ["stone"] = new Color(0.5f, 0.5f, 0.5f),
        ["stoneDark"] = new Color(0.36f, 0.36f, 0.38f),
        ["water"] = new Color(0.22f, 0.36f, 0.4f),
    };

    static readonly Dictionary<Material, Material> _copias = new Dictionary<Material, Material>();

    /// Cambia los materiales de todo lo que cuelga de "raiz" (excepto personajes, que se ven mejor con su color).
    public static void Aplicar(GameObject raiz, bool mazmorra)
    {
        Directory.CreateDirectory(Carpeta);
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            // Personajes, enemigos y lo que se recoge conservan su color: tienen que destacar.
            if (r.GetComponentInParent<ControlJugador>() || r.GetComponentInParent<Npc>() || r.GetComponentInParent<Cartel>()
                || r.GetComponentInParent<Enemigo>() || r.GetComponentInParent<Recogible>()) continue;
            var mats = r.sharedMaterials;
            bool cambia = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                var c = Copia(mats[i], mazmorra);
                if (c != mats[i]) { mats[i] = c; cambia = true; }
            }
            if (cambia) r.sharedMaterials = mats;
        }
    }

    static Material Copia(Material m, bool mazmorra)
    {
        if (m.name.StartsWith("amb-")) return m;
        if (_copias.TryGetValue(m, out var hecha)) return hecha;
        // Varios packs llaman "colormap" a su material: el nombre lleva la carpeta de su textura.
        string pack = m.mainTexture ? Pack(AssetDatabase.GetAssetPath(m.mainTexture)) + "-" : "";
        string nombre = "amb-" + (mazmorra ? "m-" : "") + pack + Limpio(m.name);
        string ruta = Carpeta + nombre + ".mat";
        var c = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (!c) { c = new Material(m); AssetDatabase.CreateAsset(c, ruta); }
        c.CopyPropertiesFromMaterial(m);
        c.name = nombre;
        if (m.HasProperty("_Color")) c.color = Tono(m.name, m.color, mazmorra);
        // Los packs con textura de colores (Mini Dungeon, Castle, Graveyard) llevan el color en la textura:
        // se usa una copia desaturada (los tejados azul chillón del Castle Kit, por ejemplo).
        if (m.mainTexture is Texture2D tex && AssetDatabase.GetAssetPath(tex).Contains("/Kenney/"))
        {
            c.mainTexture = TexturaSobria(tex, mazmorra);
            c.color = mazmorra ? new Color(0.66f, 0.63f, 0.68f) : new Color(0.92f, 0.9f, 0.88f);
        }
        EditorUtility.SetDirty(c);
        _copias[m] = c;
        return c;
    }

    /// Menos saturado y algo más oscuro; los materiales conocidos, a su color fijo.
    static Color Tono(string nombre, Color c, bool mazmorra)
    {
        foreach (var f in Fijos) if (Limpio(nombre) == f.Key) return f.Value;
        float gris = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        var d = Color.Lerp(new Color(gris, gris, gris, c.a), c, 0.6f) * (mazmorra ? 0.8f : 0.86f);
        d.a = c.a;
        return d;
    }

    static readonly Dictionary<Texture2D, Texture2D> _texturas = new Dictionary<Texture2D, Texture2D>();

    /// Copia de la textura con la saturación al 55 % (se guarda como PNG junto a los materiales).
    static Texture2D TexturaSobria(Texture2D original, bool mazmorra)
    {
        if (_texturas.TryGetValue(original, out var hecha)) return hecha;
        string origen = AssetDatabase.GetAssetPath(original);
        string ruta = Carpeta + "tex-" + Pack(origen) + "-" + Path.GetFileNameWithoutExtension(origen) + ".png";
        if (!File.Exists(ruta))
        {
            var t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(origen));
            var px = t.GetPixels();
            for (int i = 0; i < px.Length; i++)
            {
                var p = px[i];
                float gris = p.r * 0.3f + p.g * 0.59f + p.b * 0.11f;
                var d = Color.Lerp(new Color(gris, gris, gris), p, 0.55f);
                d.a = p.a;
                px[i] = d;
            }
            t.SetPixels(px);
            File.WriteAllBytes(ruta, t.EncodeToPNG());
            AssetDatabase.ImportAsset(ruta);
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            var impOrigen = AssetImporter.GetAtPath(origen) as TextureImporter;
            if (imp && impOrigen) { imp.filterMode = impOrigen.filterMode; imp.mipmapEnabled = impOrigen.mipmapEnabled; imp.SaveAndReimport(); }
        }
        var r = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        _texturas[original] = r;
        return r;
    }

    /// "Assets/Kenney/CastleKit/Textures/colormap.png" → "CastleKit".
    static string Pack(string ruta)
    {
        var partes = ruta.Split('/');
        int i = System.Array.IndexOf(partes, "Kenney");
        return i >= 0 && i + 1 < partes.Length ? partes[i + 1] : "otro";
    }

    static string Limpio(string n) => n.Replace(" (Instance)", "").Replace("/", "_").Trim();
}
