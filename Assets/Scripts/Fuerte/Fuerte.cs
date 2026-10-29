using System.Collections.Generic;
using UnityEngine;

/// El fuerte en una escena: levanta las piezas del diseño sobre fuerte-base.
/// En la escena Fuerte usa el diseño propio; en el pueblo (hueco) también, solo para verlo;
/// en el asalto, el del rival (Fuerte.DisenoAsalto).
public class Fuerte : MonoBehaviour
{
    public PiezaFuerte.Modo modo = PiezaFuerte.Modo.Propio;
    [Tooltip("Raíz del mapa fuerte-base en la escena.")]
    public Transform origen;
    [Tooltip("fuerte-base.txt, para las reglas de construcción.")]
    public TextAsset mapaBase;

    /// Diseño del rival para la escena Asalto (lo pone quien busca el rival).
    public static DisenoFuerte DisenoAsalto;

    public static Fuerte Actual { get; private set; }
    public MapaTexto Mapa { get; private set; }
    public List<PiezaFuerte> Piezas { get; private set; } = new List<PiezaFuerte>();

    public DisenoFuerte Diseno => modo == PiezaFuerte.Modo.Asalto ? (DisenoAsalto ?? new DisenoFuerte()) : Partida.Actual.fuerte;

    void Awake()
    {
        if (modo != PiezaFuerte.Modo.Vista) Actual = this;
        Mapa = mapaBase ? CargadorMapa.Leer(mapaBase.text) : null;
    }

    void Start() => Reconstruir();

    public void Reconstruir()
    {
        if (!origen) origen = transform;
        Piezas = ConstructorFuerte.Construir(Diseno, origen, modo);
    }

    /// Posición en el mundo de la casilla (x, z) del fuerte.
    public Vector3 Mundo(int x, int z) => origen.TransformPoint(ReglasFuerte.Celda(x, z));

    /// Casilla (x, z) más cercana a un punto del mundo.
    public Vector2Int Casilla(Vector3 mundo)
    {
        var l = origen.InverseTransformPoint(mundo);
        return new Vector2Int(Mathf.RoundToInt(l.x), Mathf.RoundToInt(l.z));
    }
}
