using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Icono de la app y logo de la pantalla de arranque (tarea 9.1). Fotografía el orco de Mini Dungeon
/// sobre un degradado y deja Assets/Iconos/icono.png (1024x1024) y logo.png (1024x512, con el nombre).
/// Los pone como icono por defecto y como logo del arranque de Unity (sin el logo de Unity).
/// Menú Prototipo > Crear icono, o batch -executeMethod CrearIcono.EnBatch.
public static class CrearIcono
{
    public const string RutaIcono = "Assets/Iconos/icono.png";
    public const string RutaLogo = "Assets/Iconos/logo.png";
    static readonly Color Arriba = new Color(0.36f, 0.55f, 0.24f), Abajo = new Color(0.12f, 0.2f, 0.1f);

    [MenuItem("Prototipo/Crear icono")]
    public static void Crear()
    {
        var escenaAntes = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.type = LightType.Directional; luz.intensity = 1.25f;
        luz.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

        var orco = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CrearPrefabs.Dungeon + "character-orc.fbx"));
        orco.transform.rotation = Quaternion.Euler(0f, 200f, 0f);   // mira a la cámara, un poco girado
        var b = orco.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });

        // Icono: el orco entero, con margen para el recorte redondo de Android.
        var mira = new Vector3(b.center.x, b.min.y + b.size.y * 0.52f, b.center.z);
        var icono = Foto(1024, 1024, mira + new Vector3(0f, 0.05f, -b.size.y * 2.9f), mira, 26f);
        Guardar(icono, RutaIcono);

        // Logo: orco entero a la izquierda y el nombre a la derecha.
        var texto = new GameObject("Nombre").AddComponent<TextMesh>();
        texto.text = "ORKHOLD"; texto.fontSize = 120; texto.characterSize = 0.015f * b.size.y;
        texto.anchor = TextAnchor.MiddleLeft; texto.fontStyle = FontStyle.Bold;
        texto.color = new Color(1f, 0.86f, 0.45f);
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.GetComponent<MeshRenderer>().sharedMaterial = texto.font.material;
        texto.transform.position = new Vector3(b.center.x + b.size.x * 0.5f, b.center.y, b.center.z - b.size.z);
        var centroLogo = new Vector3(b.center.x + b.size.y * 0.75f, b.center.y, b.center.z);
        var logo = Foto(1024, 512, centroLogo + new Vector3(0f, 0f, -b.size.y * 3.2f), centroLogo, 24f);
        Guardar(logo, RutaLogo);

        Object.DestroyImmediate(orco);
        AssetDatabase.Refresh();
        Importar(RutaIcono, false);
        Importar(RutaLogo, true);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaIcono);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.backgroundColor = Abajo;
        PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, AssetDatabase.LoadAssetAtPath<Sprite>(RutaLogo)) };
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(escenaAntes)) EditorSceneManager.OpenScene(escenaAntes);
        Debug.Log("Icono creado: " + RutaIcono + " y " + RutaLogo);
    }

    /// Foto con fondo degradado: se dibuja el degradado y encima lo que ve la cámara (fondo transparente).
    static Texture2D Foto(int ancho, int alto, Vector3 desde, Vector3 mira, float campo)
    {
        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(mira - desde));
        cam.fieldOfView = campo; cam.nearClipPlane = 0.01f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        cam.targetTexture = rt; cam.Render();
        RenderTexture.active = rt;
        var frente = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        frente.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        RenderTexture.active = null;
        Object.DestroyImmediate(cam.gameObject); Object.DestroyImmediate(rt);

        var t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false);
        var px = frente.GetPixels();
        for (int y = 0; y < alto; y++)
        {
            var fondo = Color.Lerp(Abajo, Arriba, y / (float)(alto - 1));
            for (int x = 0; x < ancho; x++)
            {
                // Viñeta suave hacia las esquinas.
                float dx = x / (float)ancho - 0.5f, dy = y / (float)alto - 0.5f;
                var f = Color.Lerp(fondo, Abajo * 0.7f, Mathf.Clamp01((dx * dx + dy * dy) * 1.6f));
                var c = px[y * ancho + x];
                var r = Color.Lerp(f, new Color(c.r, c.g, c.b), c.a); r.a = 1f;
                px[y * ancho + x] = r;
            }
        }
        t.SetPixels(px); t.Apply();
        Object.DestroyImmediate(frente);
        return t;
    }

    static void Guardar(Texture2D t, string ruta)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        Object.DestroyImmediate(t);
    }

    static void Importar(string ruta, bool sprite)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 1024;
        imp.SaveAndReimport();
    }

    public static void EnBatch()
    {
        try { Crear(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
