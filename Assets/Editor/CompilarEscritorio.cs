using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Compilaciones de escritorio para la red del pueblo (fase 7):
/// - Windows (Builds/Windows/Orkhold.exe): cliente con ventana, o servidor y bots sin ventana
///   (-batchmode -nographics -servidor / -bot). Lo usa pruebas/red-local.ps1.
/// - Servidor dedicado Linux (Builds/Servidor/orkhold-mundo.x86_64): solo el pueblo, para el contenedor
///   de codehive. Necesita el módulo "Linux Dedicated Server Build Support" de Unity.
public static class CompilarEscritorio
{
    static string[] Escenas => EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();

    [MenuItem("Prototipo/Compilar Windows (pruebas de red)")]
    public static bool Windows()
    {
        if (!File.Exists(MontarEscenas.CarpetaEscenas + "Pueblo.unity")) MontarEscenas.MontarTodas();
        return Compilar(new BuildPlayerOptions
        {
            scenes = Escenas,
            locationPathName = "Builds/Windows/Orkhold.exe",
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
        }, "Windows");
    }

    [MenuItem("Prototipo/Compilar servidor Linux")]
    public static bool ServidorLinux()
    {
        if (!File.Exists(MontarEscenas.CarpetaEscenas + "Pueblo.unity")) MontarEscenas.MontarTodas();
        // El servidor arranca directamente en el pueblo.
        return Compilar(new BuildPlayerOptions
        {
            scenes = new[] { MontarEscenas.CarpetaEscenas + "Pueblo.unity" },
            locationPathName = "Builds/Servidor/orkhold-mundo.x86_64",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
        }, "Servidor Linux");
    }

    static bool Compilar(BuildPlayerOptions o, string nombre)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(o.locationPathName));
        var r = BuildPipeline.BuildPlayer(o).summary;
        Debug.Log($"{nombre}: resultado={r.result} tamaño={r.totalSize / 1048576} MB errores={r.totalErrors} tiempo={r.totalTime.TotalSeconds:F0}s ruta={o.locationPathName}");
        return r.result == BuildResult.Succeeded;
    }

    public static void WindowsEnBatch() => EditorApplication.Exit(Windows() ? 0 : 1);
    public static void ServidorLinuxEnBatch() => EditorApplication.Exit(ServidorLinux() ? 0 : 1);
}
