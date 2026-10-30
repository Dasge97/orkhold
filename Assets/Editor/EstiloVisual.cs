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
        string nombre = "amb-" + (mazmorra ? "m-" : "") + Limpio(m.name);
        string ruta = Carpeta + nombre + ".mat";
        var c = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (!c) { c = new Material(m); AssetDatabase.CreateAsset(c, ruta); }
        c.CopyPropertiesFromMaterial(m);
        c.name = nombre;
        if (m.HasProperty("_Color")) c.color = Tono(m.name, m.color, mazmorra);
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

    static string Limpio(string n) => n.Replace(" (Instance)", "").Replace("/", "_").Trim();
}
