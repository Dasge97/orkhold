using UnityEngine;

/// Prefabs que el código necesita en tiempo de juego (lo que suelta un árbol, un enemigo...).
/// Vive en Assets/Resources/Catalogo.asset; lo rellena CrearPrefabs.
[CreateAssetMenu(menuName = "Orkhold/Catálogo", fileName = "Catalogo")]
public class Catalogo : ScriptableObject
{
    public GameObject moneda, madera, piedra, hierro, hueso, pocion;

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

    /// Pone en el suelo el recogible de un recurso. Devuelve null si el catálogo no lo tiene.
    public static GameObject Soltar(Recurso r, Vector3 donde, int cantidad = 1)
    {
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
