using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// Funde las mallas fijas de un mapa (suelo, caminos, muros, casas, flores...) en pocas mallas
/// grandes, una por trozo de N x N casillas, con una submalla por material. El móvil dibuja y
/// descarta así unas decenas de objetos en vez de miles. Las colisiones se quedan donde estaban.
public static class CombinarMallas
{
    public const string Carpeta = "Assets/Mapas/Mallas/";

    /// "excluir": objetos (y sus hijos) que no se tocan, como árboles, rocas y entidades.
    static bool BajoSuelo(Transform t, Transform raiz)
    {
        for (var x = t; x && x != raiz; x = x.parent) if (x.name == "Suelo") return true;
        return false;
    }

    public static int Combinar(Transform raiz, string nombre, ICollection<GameObject> excluir, int trozo = 16)
    {
        var excluidos = new HashSet<Transform>();
        foreach (var g in excluir) if (g) excluidos.Add(g.transform);
        bool Excluido(Transform t)
        {
            for (var x = t; x && x != raiz.parent; x = x.parent)
                if (excluidos.Contains(x) || x.name == "Entidades") return true;
            return false;
        }

        var filtros = raiz.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => f.sharedMesh && f.GetComponent<MeshRenderer>() && !Excluido(f.transform)).ToList();

        // Por trozo, por si proyecta sombra y por material. El suelo y lo muy bajo (flores, hierba)
        // no proyectan sombra: no se ve y en el móvil la pasada de sombras es lo más caro (tarea 9.3).
        var grupos = new Dictionary<Vector3Int, Dictionary<Material, List<CombineInstance>>>();
        foreach (var f in filtros)
        {
            var mr = f.GetComponent<MeshRenderer>();
            var p = raiz.InverseTransformPoint(f.transform.position);
            bool sombra = !BajoSuelo(f.transform, raiz) && mr.bounds.size.y > 0.4f;
            var clave = new Vector3Int(Mathf.FloorToInt((p.x + 0.5f) / trozo), Mathf.FloorToInt((p.z + 0.5f) / trozo), sombra ? 1 : 0);
            if (!grupos.TryGetValue(clave, out var porMat)) grupos[clave] = porMat = new Dictionary<Material, List<CombineInstance>>();
            var mats = mr.sharedMaterials;
            for (int s = 0; s < f.sharedMesh.subMeshCount && s < mats.Length; s++)
            {
                if (!mats[s]) continue;
                if (!porMat.TryGetValue(mats[s], out var lista)) porMat[mats[s]] = lista = new List<CombineInstance>();
                lista.Add(new CombineInstance { mesh = f.sharedMesh, subMeshIndex = s, transform = raiz.worldToLocalMatrix * f.transform.localToWorldMatrix });
            }
        }

        string carpeta = Carpeta + nombre + "/";
        if (Directory.Exists(carpeta)) AssetDatabase.DeleteAsset(carpeta.TrimEnd('/'));
        Directory.CreateDirectory(carpeta);
        var contenedor = new GameObject("Mallas fundidas");
        contenedor.transform.SetParent(raiz, false);
        int n = 0;
        foreach (var kv in grupos)
        {
            var mats = kv.Value.Keys.ToArray();
            var partes = new List<CombineInstance>();
            foreach (var m in mats)
            {
                var sub = new Mesh { indexFormat = IndexFormat.UInt32 };
                sub.CombineMeshes(kv.Value[m].ToArray(), true, true);
                partes.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
            }
            var malla = new Mesh { name = $"{nombre}_{kv.Key.x}_{kv.Key.y}" + (kv.Key.z == 1 ? "" : "_sinsombra"), indexFormat = IndexFormat.UInt32 };
            malla.CombineMeshes(partes.ToArray(), false, true);
            malla.RecalculateBounds();
            foreach (var pt in partes) Object.DestroyImmediate(pt.mesh);
            AssetDatabase.CreateAsset(malla, carpeta + malla.name + ".asset");

            var g = new GameObject("Trozo " + kv.Key);
            g.transform.SetParent(contenedor.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = malla;
            var mr = g.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            if (kv.Key.z == 0) mr.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
            n++;
        }

        // Quitar los originales: renderers fuera; objetos que se quedan vacíos (sin colisión ni hijos útiles), fuera.
        // Antes se desempaquetan los prefabs de modelo para poder tocarlos por dentro.
        foreach (var f in filtros)
        {
            if (!f) continue;
            var r = PrefabUtility.GetOutermostPrefabInstanceRoot(f.gameObject);
            if (r && !Excluido(r.transform)) PrefabUtility.UnpackPrefabInstance(r, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        foreach (var f in filtros)
        {
            if (!f) continue;
            Object.DestroyImmediate(f.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(f);
        }
        Podar(raiz, excluidos);
        AssetDatabase.SaveAssets();
        return n;
    }

    /// Borra hijos sin componentes útiles y sin descendientes útiles.
    static bool Podar(Transform t, HashSet<Transform> excluidos)
    {
        bool util = excluidos.Contains(t) || t.name == "Entidades" || t.name == "Mallas fundidas"
                    || t.GetComponents<Component>().Any(c => !(c is Transform));
        for (int i = t.childCount - 1; i >= 0; i--)
            if (Podar(t.GetChild(i), excluidos)) util = true;
        // Dentro de un prefab solo se puede borrar su raíz entera.
        bool sePuedeBorrar = !PrefabUtility.IsPartOfPrefabInstance(t.gameObject) || PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject);
        if (!util && t.parent && sePuedeBorrar) Object.DestroyImmediate(t.gameObject);
        return util;
    }
}
