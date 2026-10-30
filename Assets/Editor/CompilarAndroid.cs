using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Compila el APK de Android. Se dispara solo si existe Revision/compilar.txt
/// (y sobrevive al cambio de plataforma, que recarga los scripts) o desde Prototipo > Compilar APK.
[InitializeOnLoad]
public static class CompilarAndroid
{
    const string Marca = "Revision/compilar.txt";
    const string Salida = "Builds/orkhold.apk";

    static CompilarAndroid()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Marca) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            Compilar();
        };
    }

    public static BuildResult UltimoResultado = BuildResult.Unknown;

    /// Deja la URL de la API como estaba antes de compilar (en git va vacía: modo local).
    static void RestaurarUrl(string valor)
    {
        var c = AssetDatabase.LoadAssetAtPath<Configuracion>(CrearPrefabs.RutaConfiguracion);
        if (!c) return;
        c.urlApi = valor;
        EditorUtility.SetDirty(c);
        AssetDatabase.SaveAssets();
        Debug.Log("APK: urlApi restaurada a '" + valor + "'");
    }

    /// Valor del argumento de línea de órdenes "nombre valor", o null si no está.
    static string Argumento(string nombre)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == nombre) return a[i + 1];
        return null;
    }

    /// Para la línea de órdenes (lanzar Unity con -buildTarget Android): sale con 0 si compila.
    public static void EnBatch()
    {
        try { Compilar(); }
        catch (System.Exception e) { Debug.LogException(e); }
        EditorApplication.Exit(UltimoResultado == BuildResult.Succeeded ? 0 : 1);
    }

    [MenuItem("Prototipo/Compilar APK")]
    public static void Compilar()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            // El cambio de plataforma reimporta y recarga los scripts; la marca sigue en disco
            // y el constructor estático volverá a llamar aquí con Android ya activo.
            Debug.Log("Cambiando la plataforma a Android...");
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
            return;
        }
        if (File.Exists(Marca)) File.Delete(Marca);
        // Las escenas no van en git: se montan desde los mapas de texto si faltan.
        if (!File.Exists(MontarEscenas.CarpetaEscenas + "Pueblo.unity")) MontarEscenas.MontarTodas();

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        // Solo ARM64: Unity 6 ya no compila x86_64 para Android. El emulador (x86_64) lo ejecuta
        // con su traducción de ARM, que funciona pero va más lento que un móvil real.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // OpenGL ES 3 en vez de Vulkan: el Vulkan del emulador se cae (VK_ERROR_DEVICE_LOST).
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.Android.useCustomKeystore = false;
        // URL de la API: argumento -urlApi (vacío = modo local). En http (emulador) hay que permitir tráfico sin cifrar.
        string url = Argumento("-urlApi");
        var conf = AssetDatabase.LoadAssetAtPath<Configuracion>(CrearPrefabs.RutaConfiguracion);
        string urlAntes = conf ? conf.urlApi : "";
        if (url != null && conf) { conf.urlApi = url; EditorUtility.SetDirty(conf); AssetDatabase.SaveAssets(); }
        PlayerSettings.insecureHttpOption = (conf && conf.urlApi.StartsWith("http://")) ? InsecureHttpOption.AlwaysAllowed : InsecureHttpOption.NotAllowed;
        Debug.Log("APK: urlApi=" + (conf ? conf.urlApi : "(sin configuración)"));
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory("Builds");
        var opciones = new BuildPlayerOptions
        {
            scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, e => e.path),
            locationPathName = Salida,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport informe;
        try { informe = BuildPipeline.BuildPlayer(opciones); }
        catch { if (url != null) RestaurarUrl(urlAntes); throw; }
        var r = informe.summary;
        UltimoResultado = r.result;
        // La compilación recarga los assets: la referencia "conf" ya no vale, se vuelve a cargar.
        if (url != null) RestaurarUrl(urlAntes);
        Debug.Log($"APK: resultado={r.result} tamaño={r.totalSize / 1048576} MB errores={r.totalErrors} tiempo={r.totalTime.TotalSeconds:F0}s ruta={Salida}");
    }
}
