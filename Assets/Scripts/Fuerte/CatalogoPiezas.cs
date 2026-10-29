using System;
using System.Collections.Generic;
using UnityEngine;

/// Piezas del fuerte (01-diseno.md §5.1): modelo, coste, vida y límites.
/// Vive en Assets/Resources/CatalogoPiezas.asset; lo rellena CrearPrefabs.
[CreateAssetMenu(menuName = "Orkhold/Catálogo de piezas", fileName = "CatalogoPiezas")]
public class CatalogoPiezas : ScriptableObject
{
    public enum Categoria { Muro, Trampa, Defensor }

    [Serializable]
    public class Coste
    {
        public Recurso recurso;
        public int cantidad;
    }

    [Serializable]
    public class Pieza
    {
        public string id;
        public string nombre;
        public Categoria categoria;
        public GameObject prefab;
        public List<Coste> costes = new List<Coste>();
        [Tooltip("Vida base. 0 = no se rompe (trampa).")]
        public int vida;
        [Tooltip("Si no está vacío, no cuesta recursos: gasta una unidad de este tipo del inventario (orco jefe).")]
        public string requiereUnidad = "";
        public bool EsPuerta => id == "puerta";

        public string TextoCoste()
        {
            if (!string.IsNullOrEmpty(requiereUnidad)) return "botín";
            var partes = new List<string>();
            foreach (var c in costes) partes.Add($"{c.cantidad} {Nombres.Plural(c.recurso, c.cantidad)}");
            return string.Join(" + ", partes);
        }
    }

    [Header("Límites de la primera versión")]
    public int maxMuros = 40;
    public int maxTrampas = 6;
    public int maxDefensores = 4;
    [Header("Mejora de defensores con hueso")]
    public int huesosPorMejora = 2;
    public int mejoraMaxima = 3;

    public List<Pieza> piezas = new List<Pieza>();

    static CatalogoPiezas _instancia;
    public static CatalogoPiezas Instancia => _instancia ? _instancia : (_instancia = Resources.Load<CatalogoPiezas>("CatalogoPiezas"));

    public Pieza Buscar(string id)
    {
        foreach (var p in piezas) if (p.id == id) return p;
        return null;
    }

    public int Limite(Categoria c) => c == Categoria.Muro ? maxMuros : c == Categoria.Trampa ? maxTrampas : maxDefensores;
}
