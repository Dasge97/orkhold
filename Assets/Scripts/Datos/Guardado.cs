using System;
using System.IO;
using UnityEngine;

/// Guardado local de la partida en JSON (persistentDataPath/partida.json), con versión.
/// Se carga al arrancar el juego y se guarda medio segundo después de cada cambio, al pausar y al salir.
/// En la fase 6 pasa a ser caché: la API manda.
public class Guardado : MonoBehaviour
{
    public static string Ruta = "";
    /// Falso en los tests que no deben tocar el disco.
    public static bool Activo = true;
    public static bool Pendiente { get; private set; }
    public static string UltimoError { get; private set; } = "";

    static Guardado _instancia;
    float _guardarEn;

    public static string RutaPorDefecto => Path.Combine(Application.persistentDataPath, "partida.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        if (string.IsNullOrEmpty(Ruta)) Ruta = RutaPorDefecto;
        Cargar();
        var go = new GameObject("Guardado");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<Guardado>();
        Partida.AlCambiar += Marcar;
    }

    static void Marcar()
    {
        Pendiente = true;
        if (_instancia) _instancia._guardarEn = Time.unscaledTime + 0.5f;
    }

    void Update()
    {
        if (Pendiente && Time.unscaledTime >= _guardarEn) Guardar();
    }

    void OnApplicationPause(bool pausa) { if (pausa && Pendiente) Guardar(); }
    void OnApplicationQuit() { if (Pendiente) Guardar(); }

    /// Lee el fichero. Si no existe o está roto, partida nueva (el roto se aparta como .roto).
    public static void Cargar()
    {
        if (!Activo) return;
        try
        {
            if (!File.Exists(Ruta)) { Partida.Reemplazar(new DatosPartida()); Pendiente = false; return; }
            var datos = JsonUtility.FromJson<DatosPartida>(File.ReadAllText(Ruta));
            if (datos == null) throw new Exception("fichero vacío");
            Migrar(datos);
            Partida.Reemplazar(datos);
            Pendiente = false;
            Debug.Log($"Partida cargada de {Ruta}: {datos.fuerte.piezas.Count} piezas, {datos.inventario.monedas} monedas");
        }
        catch (Exception e)
        {
            UltimoError = e.Message;
            Debug.LogWarning("No se pudo leer la partida (" + e.Message + "); empiezo una nueva.");
            try { File.Copy(Ruta, Ruta + ".roto", true); } catch { }
            Partida.Reemplazar(new DatosPartida());
        }
    }

    /// Escribe a un fichero temporal y lo cambia por el bueno, para no dejar uno a medias.
    public static void Guardar()
    {
        Pendiente = false;
        if (!Activo) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
            var tmp = Ruta + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Partida.Actual));
            if (File.Exists(Ruta)) File.Delete(Ruta);
            File.Move(tmp, Ruta);
        }
        catch (Exception e)
        {
            UltimoError = e.Message;
            Debug.LogWarning("No se pudo guardar la partida: " + e.Message);
        }
    }

    /// Datos de versiones antiguas a la actual.
    static void Migrar(DatosPartida d)
    {
        d.inventario ??= new DatosInventario();
        d.unidades ??= new System.Collections.Generic.List<DatosUnidad>();
        d.progreso ??= new System.Collections.Generic.List<DatosPartida.Par>();
        d.fuerte ??= new DisenoFuerte();
        d.fuerte.piezas ??= new System.Collections.Generic.List<DisenoFuerte.PiezaDiseno>();
        d.version = DatosPartida.VersionActual;
    }
}
