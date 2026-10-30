using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// Llamadas del servidor de mundo a la API (02-arquitectura.md §3): averigua quién es cada cliente
/// con su token de sesión (GET /jugador) y suma lo que recoge (POST /inventario/sumar con la clave de servidor).
/// URL y clave salen de las variables de entorno ORKHOLD_API y ORKHOLD_CLAVE_SERVIDOR.
public class ApiServidor : MonoBehaviour
{
    static ApiServidor _instancia;
    static ApiServidor Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = new GameObject("ApiServidor");
            DontDestroyOnLoad(go);
            return _instancia = go.AddComponent<ApiServidor>();
        }
    }

    public static string Url => (Environment.GetEnvironmentVariable("ORKHOLD_API") ?? Configuracion.Argumento("-api") ?? "").TrimEnd('/');
    static string Clave => Environment.GetEnvironmentVariable("ORKHOLD_CLAVE_SERVIDOR") ?? "";
    /// Hay API para sumar lo recogido (si no, el servidor solo lo apunta en el registro).
    public static bool Activa => !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Clave);

    [Serializable] class PeticionSumar { public string jugador, recurso, origen; public int cantidad; }

    public static void ResolverJugador(string tokenSesion, Action<string> fin)
    {
        if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(tokenSesion)) { fin(""); return; }
        Instancia.StartCoroutine(Instancia.Hacer("GET", "/jugador", null, tokenSesion, false, (ok, cuerpo) =>
        {
            fin(ok ? JsonUtility.FromJson<MensajesApi.RespuestaJugador>(cuerpo)?.jugador?.id ?? "" : "");
        }));
    }

    public static void Sumar(string jugadorId, Recurso r, int cantidad, string origen, Action<bool> fin = null)
    {
        if (!Activa || string.IsNullOrEmpty(jugadorId) || r == Recurso.Pocion) { fin?.Invoke(false); return; }
        var p = new PeticionSumar { jugador = jugadorId, recurso = Nombre(r), cantidad = cantidad, origen = origen };
        Instancia.StartCoroutine(Instancia.Hacer("POST", "/inventario/sumar", JsonUtility.ToJson(p), "", true, (ok, _) => fin?.Invoke(ok)));
    }

    /// GET /balance para el servidor de mundo (tiempos de reaparición).
    public static void DescargarBalance()
    {
        if (string.IsNullOrEmpty(Url)) return;
        Instancia.StartCoroutine(Instancia.Hacer("GET", "/balance", null, "", false, (ok, cuerpo) => { if (ok) Balance.Recibir(cuerpo); }));
    }

    [Serializable] class PeticionLatido { public int jugadores; }

    /// POST /servidor/latido: cuántos jugadores hay en el pueblo (lo enseña el panel).
    public static void Latido(int jugadores)
    {
        if (!Activa) return;
        Instancia.StartCoroutine(Instancia.Hacer("POST", "/servidor/latido", JsonUtility.ToJson(new PeticionLatido { jugadores = jugadores }), "", true, (_, __) => { }));
    }

    static string Nombre(Recurso r) => r == Recurso.Moneda ? "moneda" : r.ToString().ToLowerInvariant();

    IEnumerator Hacer(string metodo, string ruta, string cuerpo, string token, bool conClave, Action<bool, string> fin)
    {
        using var req = new UnityWebRequest(Url + ruta, metodo);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (metodo != "GET")
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo ?? "{}"));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", "Bearer " + token);
        if (conClave) req.SetRequestHeader("X-Clave-Servidor", Clave);
        req.timeout = 8;
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success;
        if (!ok) Debug.LogWarning($"ApiServidor {metodo} {ruta}: {req.responseCode} {req.downloadHandler?.text}");
        fin(ok, req.downloadHandler?.text ?? "");
    }
}
