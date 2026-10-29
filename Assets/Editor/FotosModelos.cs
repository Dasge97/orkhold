using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Hoja de fotos de modelos para ver su forma y orientación. Lee Revision/modelos.txt (una ruta
/// de Assets por línea) y deja Revision/modelos.png (cenital, cubo rojo = norte +Z) y
/// Revision/modelos-lado.png (vista en perspectiva desde el sureste), más modelos-info.txt.
/// Menú Prototipo > Fotos de modelos, o batch -executeMethod FotosModelos.EnBatch.
public static class FotosModelos
{
    [MenuItem("Prototipo/Fotos de modelos")]
    public static void Hacer()
    {
        var rutas = File.ReadAllLines("Revision/modelos.txt").Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var c in Object.FindObjectsByType<Camera>()) Object.DestroyImmediate(c.gameObject);
        int col = 6, tam = 256, filas = (rutas.Length + col - 1) / col;
        var cenital = new Texture2D(col * tam, filas * tam, TextureFormat.RGB24, false);
        var lado = new Texture2D(col * tam, filas * tam, TextureFormat.RGB24, false);
        var info = new StringBuilder();
        for (int i = 0; i < rutas.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutas[i]);
            if (!prefab) { info.AppendLine($"{i,2}: NO EXISTE {rutas[i]}"); continue; }
            var m = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var rs = m.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs.Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; }) : new Bounds();
            float lt = Mathf.Max(1f, Mathf.Max(b.size.x, b.size.z)) * 0.75f;
            var norte = GameObject.CreatePrimitive(PrimitiveType.Cube);
            norte.transform.localScale = Vector3.one * 0.12f * lt / 0.75f;
            norte.transform.position = new Vector3(0f, b.max.y + 0.1f, lt * 0.8f);
            norte.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = Color.red };
            int cx = i % col, cy = filas - 1 - i / col;
            Foto(cenital, cx, cy, tam, new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f), true, lt);
            var mira = b.center;
            var desde = mira + new Vector3(1f, 1.1f, -1f).normalized * (lt * 4f);
            Foto(lado, cx, cy, tam, desde, Quaternion.LookRotation(mira - desde), false, lt);
            info.AppendLine($"{i,2} fila {i / col} col {i % col}: {Path.GetFileNameWithoutExtension(rutas[i])} centro={b.center} tam={b.size}");
            foreach (var mat in rs.SelectMany(r => r.sharedMaterials).Where(x => x).Distinct())
                info.AppendLine($"     material {mat.name} shader={mat.shader.name} color={mat.color} textura={(mat.mainTexture ? mat.mainTexture.name : "-")} " +
                                $"emision={(mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor").ToString() : "-")} brillo={(mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : -1f)}");
            Object.DestroyImmediate(m); Object.DestroyImmediate(norte);
        }
        cenital.Apply(); lado.Apply();
        File.WriteAllBytes("Revision/modelos.png", cenital.EncodeToPNG());
        File.WriteAllBytes("Revision/modelos-lado.png", lado.EncodeToPNG());
        File.WriteAllText("Revision/modelos-info.txt", info.ToString());
        Debug.Log("Fotos de modelos hechas");
    }

    static void Foto(Texture2D hoja, int cx, int cy, int tam, Vector3 pos, Quaternion rot, bool orto, float lado)
    {
        var luz = Object.FindAnyObjectByType<Light>();
        var cam = new GameObject("c").AddComponent<Camera>();
        cam.orthographic = orto; cam.orthographicSize = lado; cam.fieldOfView = 30f;
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray;
        var rt = new RenderTexture(tam, tam, 24); cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        hoja.ReadPixels(new Rect(0, 0, tam, tam), cx * tam, cy * tam);
        RenderTexture.active = null;
        Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(rt);
    }

    public static void EnBatch()
    {
        try { Hacer(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
