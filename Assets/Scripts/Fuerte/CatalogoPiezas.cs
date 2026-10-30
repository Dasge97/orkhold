using System;
using System.Collections.Generic;
using UnityEngine;

/// Piezas del fuerte (01-diseno.md §5.1): modelo, coste, vida y límites.
/// Vive en Assets/Resources/CatalogoPiezas.asset; lo rellena CrearPrefabs.
[CreateAssetMenu(menuName = "Orkhold/Catálogo de piezas", fileName = "CatalogoPiezas")]
public class CatalogoPiezas : ScriptableObject
{
    public enum Categoria { Muro, Trampa, Defensor, Escondite }

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
        [Header("Escondites")]
        [Tooltip("Unidades que caben (cofre 30, barril 15, enterrado 10).")]
        public int capacidad;
        [Tooltip("Cuántos se pueden construir. 0 = todavía bloqueado.")]
        public int limite;
        [Tooltip("Jefe que hay que vencer para construirla (número de mazmorra; 0 = ninguno).")]
        public int mazmorra;

        /// Este jugador ya puede construirla (11-mazmorras.md §6).
        public bool Abierta => mazmorra <= 0 || Partida.Actual.Progreso($"jefe{mazmorra}_muerto") == "1";
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
    [Tooltip("Capacidad del cofre central (casilla 'K').")]
    public int capacidadCentral = 50;
    [Header("Mejora de defensores con hueso")]
    public int huesosPorMejora = 2;
    public int mejoraMaxima = 3;

    public List<Pieza> piezas = new List<Pieza>();

    static CatalogoPiezas _instancia;
    /// Copia en memoria del asset (el balance la cambia sin tocar el fichero del proyecto).
    public static CatalogoPiezas Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var asset = Resources.Load<CatalogoPiezas>("CatalogoPiezas");
            if (!asset) return null;
            _instancia = Instantiate(asset);
            _instancia.AplicarBalance();
            return _instancia;
        }
    }

    /// Prefijo de la pieza en el balance: "pieza.<id>" o, si es escondite, "escondite.<id>".
    static string PrefijoBalance(Pieza p) => (p.categoria == Categoria.Escondite ? "escondite." : "pieza.") + p.id;

    /// La próxima vez que se pida, se vuelve a copiar del asset.
    public static void Reiniciar() => _instancia = null;

    static readonly Recurso[] RecursosConCoste = { Recurso.Moneda, Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso };

    /// Costes, vidas, límites y mejoras del balance de la API (10-panel-admin.md §1.3), si lo hay.
    public void AplicarBalance()
    {
        if (!Balance.Cargado) return;
        maxMuros = Balance.Entero("fuerte.limite_muro", maxMuros);
        maxTrampas = Balance.Entero("fuerte.limite_trampa", maxTrampas);
        maxDefensores = Balance.Entero("fuerte.limite_defensor", maxDefensores);
        huesosPorMejora = Balance.Entero("fuerte.mejora_huesos", huesosPorMejora);
        mejoraMaxima = Balance.Entero("fuerte.mejora_maxima", mejoraMaxima);
        capacidadCentral = Balance.Entero("escondite.central.capacidad", capacidadCentral);
        foreach (var p in piezas)
        {
            p.mazmorra = Balance.Entero($"{PrefijoBalance(p)}.mazmorra", p.mazmorra);
            if (p.categoria != Categoria.Escondite) p.vida = Balance.Entero($"pieza.{p.id}.vida", p.vida);
            else
            {
                p.capacidad = Balance.Entero($"escondite.{p.id}.capacidad", p.capacidad);
                p.limite = Balance.Entero($"escondite.{p.id}.limite", p.limite);
            }
            if (!string.IsNullOrEmpty(p.requiereUnidad)) continue;
            // Si el balance no trae ningún coste de esta pieza, se queda el del catálogo.
            var nuevos = new List<Coste>();
            foreach (var r in RecursosConCoste)
            {
                string clave = $"{PrefijoBalance(p)}.coste.{Nombres.Clave(r)}";
                if (Balance.Tiene(clave) && Balance.Entero(clave, 0) > 0) nuevos.Add(new Coste { recurso = r, cantidad = Balance.Entero(clave, 0) });
            }
            bool alguno = false;
            foreach (var r in RecursosConCoste) alguno |= Balance.Tiene($"{PrefijoBalance(p)}.coste.{Nombres.Clave(r)}");
            if (alguno) p.costes = nuevos;
        }
    }

    public Pieza Buscar(string id)
    {
        foreach (var p in piezas) if (p.id == id) return p;
        return null;
    }

    public int Limite(Categoria c) => c == Categoria.Muro ? maxMuros : c == Categoria.Trampa ? maxTrampas : maxDefensores;

    /// Capacidad total de un diseño: el cofre central más cada escondite.
    public int Capacidad(DisenoFuerte d)
    {
        int n = capacidadCentral;
        foreach (var q in d.piezas) { var p = Buscar(q.tipo); if (p != null && p.categoria == Categoria.Escondite) n += p.capacidad; }
        return n;
    }
}
