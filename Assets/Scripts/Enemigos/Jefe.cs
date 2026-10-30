using System.Collections;
using UnityEngine;

/// Jefe de una mazmorra (01-diseno.md §4.3 y 11-mazmorras.md): golpe en área cada 6 s con aviso de 1 s
/// (círculo rojo), frases al empezar y al caer y, si se dice, refuerzos al llegar a media vida. La recompensa
/// la da la API al salir (la primera vez, la del §6 de 11-mazmorras.md). Al morir, vuelta al pueblo.
/// Urzog (mazmorra 1), Grimbra (2), el Vigía Mayor (3) y Kargash (4) son esta clase con otros números.
public class Jefe : Enemigo
{
    public const string ClaveProgreso = "jefe1_muerto";
    public const string TipoUnidad = "orco_jefe";

    [Header("Mazmorra")]
    [Tooltip("Mazmorra de este jefe: decide la clave de progreso (jefe<N>_muerto) y su vida en el balance.")]
    public int numeroMazmorra = 1;
    public string fraseEntrar = "Cien años. Nadie pasó.";
    public string fraseCaer = "Eres más fuerte. Entonces mandas. Así es la ley.";
    [Tooltip("Enemigo que llama al llegar a media vida (vacío = ninguno).")]
    public GameObject refuerzo;
    public int cuantosRefuerzos;

    bool _refuerzosLlamados;

    public string Clave => $"jefe{numeroMazmorra}_muerto";
    /// Refuerzos llamados (para los tests).
    public int Refuerzos { get; private set; }

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
        if (deMazmorra && numeroMazmorra > 1)
        {
            Vida.maxima = Balance.Entero($"mazmorra{numeroMazmorra}.vida_jefe", Vida.maxima);
            Vida.actual = Vida.maxima;
        }
        Vida.AlCambiar += MirarRefuerzos;
        _siguienteArea = float.MaxValue;
    }

    protected override void Pensar(float d, Vector3 aJugador)
    {
        if (!_enCombate && d <= distanciaCombate)
        {
            _enCombate = true;
            _siguienteArea = Time.time + intervaloArea;
            if (deMazmorra) Avisos.Mostrar($"{NombreCorto}: \"{fraseEntrar}\"", 3f);
            BarraVidaJefe.Mostrar(Vida, nombre);
        }
        if (_enCombate && d <= distanciaCombate && Time.time >= _siguienteArea)
        {
            StartCoroutine(GolpeArea());
            return;
        }
        base.Pensar(d, aJugador);
    }

    /// "Urzog, Guardián de la Cripta" → "Urzog".
    public string NombreCorto => nombre.Split(',')[0];

    /// A media vida, los refuerzos (Grimbra llama a 2 herreros).
    void MirarRefuerzos()
    {
        if (_refuerzosLlamados || !refuerzo || cuantosRefuerzos <= 0 || Vida.Muerto || Vida.actual * 2 > Vida.maxima) return;
        _refuerzosLlamados = true;
        for (int i = 0; i < cuantosRefuerzos; i++)
        {
            Vector3 lado = Quaternion.Euler(0f, 360f * i / cuantosRefuerzos, 0f) * Vector3.forward * 1.8f;
            var g = Instantiate(refuerzo, transform.position + lado, transform.rotation);
            g.SetActive(true);
            var e = g.GetComponent<Enemigo>();
            if (e) { e.reaparecerSegundos = 0f; e.distanciaVer = 12f; }
            Refuerzos++;
        }
        Avisos.Mostrar($"{NombreCorto} llama a los suyos", 2f);
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
            // Con servidor, la recompensa la da la API al salir de la mazmorra.
            Avisos.Mostrar($"{NombreCorto}: \"{fraseCaer}\"" + (p.Progreso(Clave) != "1" ? " Algo nuevo te espera al salir" : " Su botín te espera al salir"), 4f);
            if (p.Progreso(Clave) != "1") p.PonerProgreso(Clave, "1");
            return;
        }
        if (numeroMazmorra > 1)
        {
            if (p.Progreso(Clave) != "1") { p.PonerProgreso(Clave, "1"); Partida.Cambiado(); }
            Avisos.Mostrar($"{NombreCorto}: \"{fraseCaer}\"", 4f);
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
