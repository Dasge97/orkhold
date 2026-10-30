using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Números del juego que vienen de la API (10-panel-admin.md §1.2): GET /balance al arrancar y al volver
/// al pueblo. La última copia se guarda en el móvil; sin ninguna, valen los del catálogo y las escenas.
/// Al cargar se aplican al catálogo de piezas y, en el pueblo, a los tiempos de reaparición.
public static class Balance
{
    const string ClaveGuardada = "balance";

    static readonly Dictionary<string, double> _valores = new Dictionary<string, double>();

    public static long Version { get; private set; }
    public static bool Cargado => _valores.Count > 0;
    /// Cuando llega un balance nuevo (para refrescar pantallas con costes).
    public static event Action AlCambiar;

    [Serializable] public class Par { public string clave; public double valor; }
    [Serializable] public class Respuesta { public long version; public List<Par> lista; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        _valores.Clear(); Version = 0;
        try { Leer(PlayerPrefs.GetString(ClaveGuardada, "")); } catch (Exception e) { Debug.LogWarning("Balance guardado ilegible: " + e.Message); }
        SceneManager.sceneLoaded += (_, __) => AplicarEscena();
    }

    public static bool Tiene(string clave) => _valores.ContainsKey(clave);
    public static double Valor(string clave, double porDefecto) => _valores.TryGetValue(clave, out var v) ? v : porDefecto;
    public static int Entero(string clave, int porDefecto) => (int)Math.Round(Valor(clave, porDefecto));
    public static float Real(string clave, float porDefecto) => (float)Valor(clave, porDefecto);

    /// GET /balance (o, en el servidor de mundo, con ApiServidor). "fin(true)" si llegó uno.
    public static void Descargar(Action<bool> fin = null)
    {
        if (!Sesion.Conectado && Configuracion.ModoLocal) { fin?.Invoke(false); return; }
        ApiCliente.Get("/balance", r =>
        {
            if (r.Ok) Recibir(r.cuerpo);
            fin?.Invoke(r.Ok);
        });
    }

    /// Un JSON de GET /balance: se aplica y se guarda.
    public static void Recibir(string json)
    {
        try
        {
            Leer(json);
            PlayerPrefs.SetString(ClaveGuardada, json);
            AlCambiar?.Invoke();
            AplicarEscena();
        }
        catch (Exception e) { Debug.LogWarning("Balance no válido: " + e.Message); }
    }

    /// Para los tests: un balance a mano.
    public static void Poner(string clave, double valor) { _valores[clave] = valor; CatalogoPiezas.Instancia?.AplicarBalance(); AlCambiar?.Invoke(); }

    /// Sin balance: vuelven los números del catálogo y de las escenas (tests y cerrar sesión).
    public static void Olvidar() { _valores.Clear(); Version = 0; PlayerPrefs.DeleteKey(ClaveGuardada); CatalogoPiezas.Reiniciar(); }

    static void Leer(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var r = JsonUtility.FromJson<Respuesta>(json);
        if (r?.lista == null || r.lista.Count == 0) return;
        _valores.Clear();
        foreach (var p in r.lista) _valores[p.clave] = p.valor;
        Version = r.version;
        CatalogoPiezas.Instancia?.AplicarBalance();
    }

    /// En el pueblo: enemigos y árboles o rocas reaparecen con los tiempos del balance.
    public static void AplicarEscena()
    {
        if (!Cargado || !GestorEscena.Actual || !GestorEscena.Actual.esPueblo) return;
        float enemigos = Real("mundo.enemigos_reaparecer", 60f), recursos = Real("mundo.recursos_reaparecer", 300f);
        foreach (var e in UnityEngine.Object.FindObjectsByType<Enemigo>(FindObjectsInactive.Include))
            if (e.reaparecerSegundos > 0f) e.reaparecerSegundos = enemigos;
        foreach (var f in UnityEngine.Object.FindObjectsByType<FuenteRecurso>(FindObjectsInactive.Include)) f.segundosAgotado = recursos;
    }
}
