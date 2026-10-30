using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Iconos de los recursos y retratos de los personajes para la interfaz (15-interfaz.md §3 y §5): se fotografían
/// los propios modelos con fondo transparente y se guardan en Resources/Iconos (moneda.png, retrato-Maela.png...).
/// Lo llama MontarEscenas antes de montar; también menú Prototipo > Crear iconos del juego.
public static class CrearIconosJuego
{
    const string Carpeta = "Assets/Resources/Iconos/";

    [MenuItem("Prototipo/Crear iconos del juego")]
    public static void Crear()
    {
        Directory.CreateDirectory(Carpeta);
        var escenaAntes = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.type = LightType.Directional; luz.intensity = 1.2f;
        luz.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.62f);

        // Recursos: el recogible entero, un poco desde arriba.
        foreach (var (prefab, nombre) in new[] { ("Moneda", "moneda"), ("Madera", "madera"), ("Piedra", "piedra"),
                                                 ("Hierro", "hierro"), ("Hueso", "hueso"), ("Pocion", "pocion") })
            Fotografiar(CrearPrefabs.Cargar(prefab), nombre, entero: true);
        // Retratos: la cabeza de cada personaje, de frente.
        foreach (var n in new[] { "Maela", "Tobin", "Ferra", "Ansel", "Rook", "Gruk" })
            Fotografiar(CrearPrefabs.Cargar("Npc" + n), "retrato-" + n, entero: false);
        foreach (var id in new[] { "character-male-a", "character-female-a", "character-male-c", "character-female-c" })
            Fotografiar(AssetDatabase.LoadAssetAtPath<GameObject>(CrearPrefabs.Personajes + id + ".fbx"), "retrato-" + id, entero: false);

        AssetDatabase.Refresh();
        if (!string.IsNullOrEmpty(escenaAntes)) EditorSceneManager.OpenScene(escenaAntes);
    }

    static void Fotografiar(GameObject prefab, string nombre, bool entero)
    {
        if (!prefab) { Debug.LogWarning("CrearIconosJuego: no existe el modelo para " + nombre); return; }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.transform.rotation = Quaternion.Euler(0f, entero ? 200f : 180f, 0f);
        // Sin scripts que se pongan a hacer cosas.
        foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        var rs = g.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) { Object.DestroyImmediate(g); return; }
        var b = rs.Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
        Vector3 mira; float dist;
        if (entero) { mira = b.center; dist = Mathf.Max(b.size.x, b.size.y, b.size.z) * 1.95f; }
        else { mira = new Vector3(b.center.x, b.max.y - b.size.y * 0.27f, b.center.z); dist = b.size.y * 1.5f; }
        var desde = mira + (entero ? new Vector3(0f, 0.6f, -1f).normalized : Vector3.back) * dist;

        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(mira - desde));
        cam.fieldOfView = 30f; cam.nearClipPlane = 0.01f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Carpeta + nombre + ".png", tex.EncodeToPNG());
        Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(g);
        rt.Release();
    }
}
