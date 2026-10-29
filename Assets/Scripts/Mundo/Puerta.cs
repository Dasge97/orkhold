using UnityEngine;

/// Paso a otra escena. Puede activarse al pisarla (puerta de mazmorra, salida, entrada al fuerte)
/// o con el botón de interactuar (cofre PvP). Guarda en Viaje a qué puerta hay que volver.
public class Puerta : Interactuable
{
    [Tooltip("Nombre para encontrarla al volver (otras puertas lo usan en 'llegarA').")]
    public string id;
    [Tooltip("Escena a la que lleva.")]
    public string destino;
    [Tooltip("Id de la puerta de la escena de destino junto a la que se aparece. Vacío = aparición normal.")]
    public string llegarA;
    [Tooltip("Verdadero: se cruza andando. Falso: se usa con el botón de interactuar.")]
    public bool alPisar = true;
    [Tooltip("Dónde se pone al jugador cuando llega por esta puerta (casilla libre al lado, hacia dentro).")]
    public Vector3 llegada;
    public float radioPisar = 0.8f;
    [Tooltip("Icono del botón de interactuar cuando se usa con el botón (el cofre PvP muestra el de cofre).")]
    public Icono icono = Icono.Puerta;

    /// Si no es null, se llama en vez de cambiar de escena (por ejemplo, el asalto pide un rival antes).
    public System.Func<bool> antesDeIr;

    Transform _jugador;
    bool _armada;   // hay que salir de la puerta antes de que vuelva a funcionar

    public override Icono IconoBoton => icono;
    public override bool Disponible => base.Disponible && !alPisar;

    void Start()
    {
        var j = FindAnyObjectByType<ControlJugador>();
        if (j) _jugador = j.transform;
    }

    void Update()
    {
        if (!alPisar || !_jugador || Transicion.Ocupada) return;
        Vector3 d = _jugador.position - transform.position;
        // Zona cuadrada como la casilla: cuenta aunque el jugador se quede pegado al muro de al lado.
        float dist = Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z));
        if (dist > radioPisar + 0.6f) _armada = true;
        else if (_armada && dist <= radioPisar) Ir();
    }

    public override void Usar(ControlJugador jugador) { Ir(); }

    public void Ir()
    {
        if (antesDeIr != null && !antesDeIr()) return;
        _armada = false;
        Sonidos.Tocar("puerta");
        Viaje.llegarA = llegarA ?? "";
        Transicion.Ir(destino);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioPisar);
        Gizmos.DrawLine(transform.position, llegada);
    }
}
