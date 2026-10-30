using UnityEngine;

/// Dónde está la API. Vive en Assets/Resources/Configuracion.asset.
/// Sin URL, el juego funciona en modo local (todo en el móvil, como en las fases 1-5 y en los tests).
[CreateAssetMenu(menuName = "Orkhold/Configuración", fileName = "Configuracion")]
public class Configuracion : ScriptableObject
{
    [Tooltip("Ej. https://orkhold.code-hive.space. Vacío = modo local sin servidor.")]
    public string urlApi = "";

    static Configuracion _instancia;
    public static Configuracion Instancia => _instancia ? _instancia : (_instancia = Resources.Load<Configuracion>("Configuracion"));

    /// URL efectiva: la de PlayerPrefs si se ha cambiado a mano, si no la del asset.
    public static string UrlApi
    {
        get
        {
            var p = PlayerPrefs.GetString("url_api", "");
            if (!string.IsNullOrEmpty(p)) return p.TrimEnd('/');
            var c = Instancia;
            return c ? (c.urlApi ?? "").TrimEnd('/') : "";
        }
    }

    /// Sin URL de API: modo local (los tests y el desarrollo sin servidor).
    public static bool ModoLocal => string.IsNullOrEmpty(UrlApi);
}
