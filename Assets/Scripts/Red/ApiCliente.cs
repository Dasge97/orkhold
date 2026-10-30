using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// Peticiones a la API (02-arquitectura.md §2.6): UnityWebRequest, JSON con JsonUtility, token en cabecera.
/// Cada respuesta llega en un callback con el código HTTP (0 = sin conexión) y el cuerpo.
public class ApiCliente : MonoBehaviour
{
    public const int Timeout = 8;

    static ApiCliente _instancia;
    static ApiCliente Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = new GameObject("ApiCliente");
            DontDestroyOnLoad(go);
            return _instancia = go.AddComponent<ApiCliente>();
        }
    }

    /// Respuesta: código (0 sin conexión), cuerpo y, si hubo error, el mensaje para el jugador.
    public struct Respuesta
    {
        public int codigo;
        public string cuerpo;
        public bool Ok => codigo >= 200 && codigo < 300;
        public T Leer<T>() => JsonUtility.FromJson<T>(cuerpo);
        public string Mensaje
        {
            get
            {
                if (codigo == 0) return "Sin conexión con el servidor";
                try { var e = JsonUtility.FromJson<MensajesApi.Error>(cuerpo); if (!string.IsNullOrEmpty(e?.mensaje)) return e.mensaje; } catch { }
                return $"Error del servidor ({codigo})";
            }
        }
        public string CodigoError
        {
            get { try { return JsonUtility.FromJson<MensajesApi.Error>(cuerpo)?.error ?? ""; } catch { return ""; } }
        }
    }

    public static void Get(string ruta, Action<Respuesta> fin) => Enviar("GET", ruta, null, fin);
    public static void Post(string ruta, object cuerpo, Action<Respuesta> fin) => Enviar("POST", ruta, cuerpo, fin);
    public static void Put(string ruta, object cuerpo, Action<Respuesta> fin) => Enviar("PUT", ruta, cuerpo, fin);
    public static void Borrar(string ruta, object cuerpo, Action<Respuesta> fin) => Enviar("DELETE", ruta, cuerpo, fin);

    public static void Enviar(string metodo, string ruta, object cuerpo, Action<Respuesta> fin)
    {
        if (Configuracion.ModoLocal) { fin?.Invoke(new Respuesta { codigo = 0, cuerpo = "" }); return; }
        Instancia.StartCoroutine(Instancia.Hacer(metodo, ruta, cuerpo, fin));
    }

    IEnumerator Hacer(string metodo, string ruta, object cuerpo, Action<Respuesta> fin)
    {
        using var req = new UnityWebRequest(Configuracion.UrlApi + ruta, metodo);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (metodo != "GET")
        {
            // Siempre JSON (aunque sea "{}"): sin cuerpo, Unity manda otro tipo y la API responde 415.
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(cuerpo != null ? JsonUtility.ToJson(cuerpo) : "{}"));
            req.SetRequestHeader("Content-Type", "application/json");
        }
        if (!string.IsNullOrEmpty(Sesion.Token)) req.SetRequestHeader("Authorization", "Bearer " + Sesion.Token);
        req.timeout = Timeout;
        yield return req.SendWebRequest();
        var r = new Respuesta
        {
            codigo = req.result == UnityWebRequest.Result.ConnectionError ? 0 : (int)req.responseCode,
            cuerpo = req.downloadHandler?.text ?? "",
        };
        if (!r.Ok) Debug.LogWarning($"API {metodo} {ruta}: {r.codigo} {r.cuerpo}");
        // Solo "sin_sesion" es una sesión caducada; un 401 por contraseña mala no la cierra.
        if (r.codigo == 401 && r.CodigoError == "sin_sesion" && !string.IsNullOrEmpty(Sesion.Token)) Sesion.Caducada();
        fin?.Invoke(r);
    }
}
