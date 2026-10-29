using System;
using System.Collections.Generic;
using UnityEngine;

/// Traduce cada letra de un mapa de texto a lo que se pone en esa casilla:
/// un modelo (con o sin colisión) y/o una marca que atiende quien carga el mapa
/// (enemigos, recogibles, puntos de aparición, puertas...).
[CreateAssetMenu(menuName = "Orkhold/Paleta de mapa", fileName = "PaletaMapa")]
public class PaletaMapa : ScriptableObject
{
    public enum Colision { Ninguna, Caja, Capsula }

    /// Piezas que se eligen según las casillas vecinas (río, por ejemplo). Cada modelo, sin girar,
    /// une estos lados: recto norte-sur, curva sur-este, final sur, te oeste-este-sur, cruce los cuatro.
    [Serializable]
    public class PiezasUnion
    {
        public GameObject recto, curva, final, te, cruce, aislado;
        [Tooltip("Otras letras que cuentan como unidas, además de la propia (el puente para el río).")]
        public string unidaCon = "";
        [Tooltip("En vez de piezas enteras: un tramo que va del centro a cada lado unido (vallas).")]
        public GameObject brazo;
        [Tooltip("Posición, giro y escala del tramo dentro de un hueco que mira al norte.")]
        public Vector3 brazoPosicion, brazoGiro, brazoEscala = Vector3.one;
        public bool Activa => recto || brazo;
    }

    [Serializable]
    public class Entrada
    {
        public char letra;
        public string descripcion;
        [Tooltip("Variantes; se elige una al azar por casilla. Vacío = no se pone modelo.")]
        public GameObject[] modelos = new GameObject[0];
        public float escala = 1f;
        public float altura = 0f;
        public float giroY = 0f;
        public bool giroAleatorio;
        [Tooltip("Si es falso, la casilla no lleva el suelo normal debajo (caminos, por ejemplo).")]
        public bool conSuelo = true;
        public Colision colision = Colision.Ninguna;
        [Tooltip("Caja: ancho, alto, fondo. Cápsula: x = radio, y = alto.")]
        public Vector3 tamano = Vector3.one;
        [Tooltip("Lado del bloque en casillas (2 = casa de 2x2). El modelo se pone una vez, centrado.")]
        public int bloque = 1;
        [Tooltip("Si es verdadero, la casilla se devuelve como marca a quien carga el mapa.")]
        public bool marca;
        [Tooltip("Si tiene 'recto', el modelo se elige y se gira según las casillas vecinas.")]
        public PiezasUnion union = new PiezasUnion();
        [Tooltip("Prefab con lógica (puerta, orco, moneda...) que se pone en la casilla además del modelo.")]
        public GameObject entidad;
        [Tooltip("Falso si cada pieza tiene que seguir siendo un objeto aparte (árboles y rocas que se golpean).")]
        public bool combinar = true;
        [Tooltip("Aunque no se combinen, las piezas a menos de estas casillas del borde sí se funden (bosque del borde).")]
        public int combinarEnBorde = 0;
        [Tooltip("Las casillas seguidas de la misma fila comparten una sola caja de colisión.")]
        public bool unirColision;
        [Tooltip("Letra cuya pieza se pone debajo (el río bajo el puente). El modelo propio se gira para cruzarla.")]
        public char debajo;
    }

    [Tooltip("Suelo para mapas con 'tipo: exterior'.")]
    public GameObject[] sueloExterior = new GameObject[0];
    [Tooltip("Suelo para el resto de mapas (mazmorra, fuerte).")]
    public GameObject[] sueloInterior = new GameObject[0];
    [Tooltip("Modelos para 'relleno: flores' en la cabecera del mapa.")]
    public GameObject[] flores = new GameObject[0];
    [Range(0f, 1f)] public float densidadFlores = 0.06f;
    public List<Entrada> entradas = new List<Entrada>();

    Dictionary<char, Entrada> indice;

    /// Devuelve la entrada de una letra, o null si la paleta no la tiene.
    public Entrada Buscar(char letra)
    {
        if (indice == null || indice.Count != entradas.Count)
        {
            indice = new Dictionary<char, Entrada>();
            foreach (var e in entradas) indice[e.letra] = e;
        }
        return indice.TryGetValue(letra, out var r) ? r : null;
    }

    void OnValidate() { indice = null; }
}
