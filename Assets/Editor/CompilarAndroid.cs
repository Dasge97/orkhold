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
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory("Builds");
        var opciones = new BuildPlayerOptions
        {
            scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, e => e.path),
            locationPathName = Salida,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var informe = BuildPipeline.BuildPlayer(opciones);
        var r = informe.summary;
        UltimoResultado = r.result;
        Debug.Log($"APK: resultado={r.result} tamaño={r.totalSize / 1048576} MB errores={r.totalErrors} tiempo={r.totalTime.TotalSeconds:F0}s ruta={Salida}");
    }
}
