using UnityEngine;

/// Prefabs que el código necesita en tiempo de juego (lo que suelta un árbol, un enemigo...).
/// Vive en Assets/Resources/Catalogo.asset; lo rellena CrearPrefabs.
[CreateAssetMenu(menuName = "Orkhold/Catálogo", fileName = "Catalogo")]
public class Catalogo : ScriptableObject
{
    public GameObject moneda, madera, piedra, hierro, hueso, pocion;
    [Tooltip("Estandarte de la temporada (banner de Mini Dungeon): se tiñe con el color del tramo.")]
    public GameObject estandarte;
    [Tooltip("Marca de las casillas elegidas en la fase de marcar del saqueo.")]
    public GameObject marcaSaqueo;
    [Tooltip("Fuertes del sistema para asaltar sin servidor: fácil, medio, difícil (Assets/Mapas/fuertes-prueba).")]
    public TextAsset[] fuertesPrueba = new TextAsset[0];

    [Header("Personajes que se pueden elegir (Mini Characters)")]
    public string[] idsPersonajes = new string[0];
    public string[] nombresPersonajes = new string[0];
    public GameObject[] modelosPersonajes = new GameObject[0];
    public RuntimeAnimatorController[] controladoresPersonajes = new RuntimeAnimatorController[0];
    [Tooltip("Mini Characters son algo más pequeños que el humano de Mini Dungeon.")]
    public float escalaPersonajes = 1.12f;

    static Catalogo _instancia;
    public static Catalogo Instancia => _instancia ? _instancia : (_instancia = Resources.Load<Catalogo>("Catalogo"));

    public GameObject Recogible(Recurso r)
    {
        switch (r)
        {
            case Recurso.Moneda: return moneda;
            case Recurso.Madera: return madera;
            case Recurso.Piedra: return piedra;
            case Recurso.Hierro: return hierro;
            case Recurso.Hueso: return hueso;
            default: return pocion;
        }
    }

    /// En el pueblo online lo crea el servidor como objeto de red (RedPueblo).
    public static System.Func<Recurso, Vector3, int, GameObject> SoltarEnRed;

    /// Pone en el suelo el recogible de un recurso. Devuelve null si el catálogo no lo tiene.
    public static GameObject Soltar(Recurso r, Vector3 donde, int cantidad = 1)
    {
        if (SoltarEnRed != null) return SoltarEnRed(r, donde, cantidad);
        var c = Instancia;
        var p = c ? c.Recogible(r) : null;
        if (!p) return null;
        var g = Instantiate(p, donde, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        g.SetActive(true);
        var rec = g.GetComponent<Recogible>();
        if (rec) rec.cantidad = cantidad;
        return g;
    }
}
