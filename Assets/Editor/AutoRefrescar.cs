using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Unity solo detecta cambios en los ficheros cuando su ventana recibe el foco. Este script mira
/// cada 2 segundos si ha cambiado algún script, mapa o fichero de datos y fuerza la recarga, y
/// también atiende las marcas de Revision/ (pedir.txt, compilar.txt) y la escena borrada, para que
/// un modelo pueda trabajar sin que nadie toque la ventana de Unity.
[InitializeOnLoad]
public static class AutoRefrescar
{
    static readonly string[] Extensiones = { ".cs", ".txt", ".json", ".asset", ".asmdef" };
    static double _ultimaComprobacion;
    static string _huella = "";

    static AutoRefrescar()
    {
        _huella = Huella();
        EditorApplication.update += Comprobar;
    }

    static void Comprobar()
    {
        if (EditorApplication.timeSinceStartup - _ultimaComprobacion < 2.0) return;
        _ultimaComprobacion = EditorApplication.timeSinceStartup;
        if (Application.isBatchMode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;

        var huella = Huella();
        if (huella != _huella)
        {
            _huella = huella;
            Debug.Log("AutoRefrescar: cambios en disco, recargando.");
            AssetDatabase.Refresh();
            return;   // si hay scripts nuevos, el dominio se recarga y los demás scripts hacen lo suyo
        }

        // Sin cambios de scripts no hay recarga de dominio, así que las marcas se atienden aquí.
        if (!File.Exists("Assets/Scenes/Pueblo.unity") && AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Kenney/MiniDungeon/character-human.fbx") != null)
        {
            MontarEscenas.MontarTodas();
            return;
        }
        if (File.Exists("Revision/pedir.txt") && File.Exists("Assets/Scenes/Pueblo.unity"))
        {
            File.Delete("Revision/pedir.txt");
            RevisionPrototipo.Revisar();
            return;
        }
        if (File.Exists("Revision/compilar.txt"))
        {
            CompilarAndroid.Compilar();
        }
    }

    /// Resumen barato del estado de los ficheros: cuántos hay y la fecha más reciente.
    static string Huella()
    {
        try
        {
            var ficheros = Directory.EnumerateFiles("Assets", "*.*", SearchOption.AllDirectories)
                .Where(f => Extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Concat(Directory.Exists("Packages") ? new[] { "Packages/manifest.json" } : Array.Empty<string>())
                .ToList();
            long max = 0;
            foreach (var f in ficheros)
            {
                long t = File.GetLastWriteTimeUtc(f).Ticks;
                if (t > max) max = t;
            }
            return ficheros.Count + ":" + max;
        }
        catch { return _huella; }
    }
}
