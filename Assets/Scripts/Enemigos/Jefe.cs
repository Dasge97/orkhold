using System.Collections;
using UnityEngine;

/// Orco jefe de la mazmorra 1 (01-diseno.md §4.3): vida 15, daño 2, golpe en área cada 6 s con
/// aviso de 1 s (círculo rojo). La primera vez suelta la unidad "Orco jefe defensor"; después,
/// 3 huesos. Al morir, vuelta al pueblo con transición.
public class Jefe : Enemigo
{
    public const string ClaveProgreso = "jefe1_muerto";
    public const string TipoUnidad = "orco_jefe";

    public float intervaloArea = 6f;
    public float avisoArea = 1f;
    public float radioArea = 2.2f;
    public int danoArea = 2;
    [Tooltip("Distancia a la que aparece la barra de vida y empieza el golpe en área.")]
    public float distanciaCombate = 8f;
    public float segundosHastaSalir = 4f;
    public string puertaVuelta = "mazmorra1";
    [Tooltip("Falso cuando defiende un fuerte: ni botín de mazmorra ni vuelta al pueblo.")]
    public bool deMazmorra = true;

    float _siguienteArea;
    bool _enCombate;

    /// Golpes en área lanzados (para los tests).
    public int GolpesArea { get; private set; }

    protected override void Start()
    {
        base.Start();
        reaparecerSegundos = 0f;
        _siguienteArea = float.MaxValue;
    }

    protected override void Pensar(float d, Vector3 aJugador)
    {
        if (!_enCombate && d <= distanciaCombate)
        {
            _enCombate = true;
            _siguienteArea = Time.time + intervaloArea;
            BarraVidaJefe.Mostrar(Vida, nombre);
        }
        if (_enCombate && d <= distanciaCombate && Time.time >= _siguienteArea)
        {
            StartCoroutine(GolpeArea());
            return;
        }
        base.Pensar(d, aJugador);
    }

    IEnumerator GolpeArea()
    {
        GolpesArea++;
        _siguienteArea = Time.time + intervaloArea;
        _ocupadoHasta = Time.time + avisoArea + 0.5f;
        Vector3 centro = transform.position;
        AvisoArea.Crear(centro, radioArea, avisoArea);
        Animar("emote-yes", true);
        yield return new WaitForSeconds(avisoArea);
        if (Vida.Muerto) yield break;
        Animar("attack-kick-right", true);
        Sonidos.Tocar("jefe", centro);
        if (_jugador && _vidaJugador && Plano(_jugador.position - centro).magnitude <= radioArea)
            _vidaJugador.Danar(danoArea, centro);
    }

    protected override void SoltarBotin()
    {
        if (!deMazmorra) return;
        base.SoltarBotin();
        Partida.JefeMuertoEnEntrada = true;
        var p = Partida.Actual;
        if (!Configuracion.ModoLocal)
        {
            // Con servidor, la unidad o los huesos los da la API al salir de la mazmorra.
            Avisos.Mostrar(p.Progreso(ClaveProgreso) != "1"
                ? "¡Orco jefe derrotado! Consigues un Orco jefe defensor para tu fuerte"
                : "¡Orco jefe derrotado! +3 huesos", 4f);
            return;
        }
        if (p.Progreso(ClaveProgreso) != "1")
        {
            p.unidades.Add(new DatosUnidad { tipo = TipoUnidad });
            p.PonerProgreso(ClaveProgreso, "1");
            Partida.Cambiado();
            Avisos.Mostrar("¡Orco jefe derrotado! Consigues un Orco jefe defensor para tu fuerte", 4f);
        }
        else
        {
            Partida.Sumar(Recurso.Hueso, 3);
            Avisos.Mostrar("¡Orco jefe derrotado! +3 huesos", 3f);
        }
    }

    protected override void Morir()
    {
        base.Morir();
        BarraVidaJefe.Ocultar();
        if (deMazmorra) StartCoroutine(Salir());
    }

    IEnumerator Salir()
    {
        yield return new WaitForSeconds(segundosHastaSalir);
        Viaje.llegarA = puertaVuelta;
        Transicion.Ir("Pueblo");
    }
}
