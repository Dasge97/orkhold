using UnityEngine;

/// Dónde está la API. Vive en Assets/Resources/Configuracion.asset.
/// Sin URL, el juego funciona en modo local (todo en el móvil, como en las fases 1-5 y en los tests).
[CreateAssetMenu(menuName = "Orkhold/Configuración", fileName = "Configuracion")]
public class Configuracion : ScriptableObject
{
    [Tooltip("Ej. https://orkhold.code-hive.space. Vacío = modo local sin servidor.")]
    public string urlApi = "";
    [Tooltip("Servidor de mundo del pueblo (nombre o IP). Vacío = el pueblo se juega solo, sin red (modo anfitrión).")]
    public string hostMundo = "";
    public int puertoMundo = 7777;

    static Configuracion _instancia;
    public static Configuracion Instancia => _instancia ? _instancia : (_instancia = Resources.Load<Configuracion>("Configuracion"));

    /// URL efectiva: la de PlayerPrefs si se ha cambiado a mano, si no la del asset.
    public static string UrlApi
    {
        get
        {
            var arg = Argumento("-api");
            if (!string.IsNullOrEmpty(arg)) return arg.TrimEnd('/');
            var p = PlayerPrefs.GetString("url_api", "");
            if (!string.IsNullOrEmpty(p)) return p.TrimEnd('/');
            var c = Instancia;
            return c ? (c.urlApi ?? "").TrimEnd('/') : "";
        }
    }

    /// Servidor de mundo: argumento "-mundo host[:puerto]", si no el del asset.
    public static string HostMundo
    {
        get
        {
            var a = Argumento("-mundo");
            if (a != null) return a.Split(':')[0];
            var c = Instancia;
            return c ? (c.hostMundo ?? "") : "";
        }
    }

    public static ushort PuertoMundo
    {
        get
        {
            var a = Argumento("-mundo");
            if (a != null && a.Contains(":") && ushort.TryParse(a.Split(':')[1], out var p)) return p;
            var pa = Argumento("-puerto");
            if (pa != null && ushort.TryParse(pa, out var p2)) return p2;
            var c = Instancia;
            return (ushort)(c ? c.puertoMundo : 7777);
        }
    }

    /// Valor de un argumento "nombre valor" de la línea de órdenes, o null.
    public static string Argumento(string nombre)
    {
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == nombre) return a[i + 1];
        return null;
    }

    public static bool Bandera(string nombre) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), nombre) >= 0;

    /// Sin URL de API: modo local (los tests y el desarrollo sin servidor).
    public static bool ModoLocal => string.IsNullOrEmpty(UrlApi);
}
