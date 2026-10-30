using System;
using UnityEngine;

/// Reglas del asalto (01-diseno.md §5.2): 90 s de cuenta atrás; se gana tocando el cofre del tesoro
/// antes de que acabe el tiempo y sin morir; se pierde al morir, al agotar el tiempo o al salir por la
/// entrada. Cuenta los muros rotos y reparte las recompensas (provisionales en local hasta la fase 6).
[RequireComponent(typeof(Fuerte))]
public class Asalto : MonoBehaviour
{
    public const float DuracionPorDefecto = 90f;
    public float duracion = DuracionPorDefecto;
    [Tooltip("Distancia al centro del cofre que cuenta como tocarlo.")]
    public float radioCofre = 1.25f;

    public enum Fin { EnCurso, Ganado, PerdidoMuerte, PerdidoTiempo, Abandonado }

    /// Resultado de un asalto, para la pantalla final y para la API.
    [Serializable]
    public class Resultado
    {
        public bool ganado;
        public Fin motivo;
        public int segundos;
        public int murosRotos;
        public DatosInventario recompensa = new DatosInventario();
        public int puntosAntes, puntosDespues;
    }

    /// Quién decide las recompensas: en local, RecompensaLocal; con servidor, la API (Sincronizacion).
    /// Recibe el resultado y llama a "listo" con las recompensas y puntos ya puestos.
    public static Action<Resultado, Action<Resultado>> Procesar = (r, listo) => listo(RecompensaLocal(r));
    /// Al terminar (para la pantalla de resultado, la API y los tests).
    public static event Action<Resultado> AlTerminar;

    public static Asalto Actual { get; private set; }
    public Fin Estado { get; private set; } = Fin.EnCurso;
    public float Restante => Mathf.Max(0f, duracion - _t);
    public float Transcurrido => _t;
    public int MurosRotos { get; private set; }
    public Vector3 PosicionCofre { get; private set; }
    public float DistanciaInicial { get; private set; }
    public Resultado Ultimo { get; private set; }

    Fuerte _fuerte;
    ControlJugador _jugador;
    float _t;

    void Awake()
    {
        Actual = this;
        _fuerte = GetComponent<Fuerte>();
    }

    void OnEnable()
    {
        PiezaFuerte.AlRomper += ContarMuro;
        GestorEscena.AlMorirJugador += Murio;
    }

    void OnDisable()
    {
        PiezaFuerte.AlRomper -= ContarMuro;
        GestorEscena.AlMorirJugador -= Murio;
    }

    void Start()
    {
        _jugador = FindAnyObjectByType<ControlJugador>();
        var c = ReglasFuerte.Cofre(_fuerte.Mapa);
        PosicionCofre = _fuerte.Mundo(c.x, c.y);
        DistanciaInicial = _jugador ? Plano(_jugador.transform.position - PosicionCofre).magnitude : 7f;
        // Salir por la entrada es rendirse.
        foreach (var p in FindObjectsByType<Puerta>())
            if (p.destino == "Pueblo") p.antesDeIr = () => { Terminar(Fin.Abandonado); return false; };
        PantallaAsalto.Crear(this);
    }

    void Update()
    {
        if (Estado != Fin.EnCurso || Transicion.Ocupada) return;
        _t += Time.deltaTime;
        if (_jugador && !_jugador.Vida.Muerto && Plano(_jugador.transform.position - PosicionCofre).magnitude <= radioCofre)
            Terminar(Fin.Ganado);
        else if (_t >= duracion)
            Terminar(Fin.PerdidoTiempo);
    }

    /// Distancia del jugador al cofre (para la barra).
    public float Distancia => _jugador ? Plano(_jugador.transform.position - PosicionCofre).magnitude : DistanciaInicial;

    void ContarMuro(PiezaFuerte p) { if (Estado == Fin.EnCurso) MurosRotos++; }

    void Murio() { if (Estado == Fin.EnCurso) Terminar(Fin.PerdidoMuerte); }

    public void Terminar(Fin motivo)
    {
        if (Estado != Fin.EnCurso) return;
        Estado = motivo;
        // Todo se para: defensores quietos y jugador sin control.
        foreach (var e in Enemigo.Todos.ToArray()) if (e) e.enabled = false;
        if (_jugador) _jugador.enabled = false;
        EntradaJuego.MoverPantalla = Vector2.zero;

        var r = new Resultado
        {
            ganado = motivo == Fin.Ganado,
            motivo = motivo,
            segundos = Mathf.CeilToInt(_t),
            murosRotos = MurosRotos,
            puntosAntes = Partida.Actual.puntuacion,
        };
        Procesar(r, final =>
        {
            Ultimo = final;
            Sonidos.Tocar(final.ganado ? "recoger_moneda" : "morir");
            AlTerminar?.Invoke(final);
        });
    }

    /// Recompensas provisionales sin servidor: ganar 10 monedas + 2 materiales al azar y +20 puntos;
    /// perder 3 monedas y -15 puntos (la API aplica el Elo de verdad en la fase 6).
    public static Resultado RecompensaLocal(Resultado r)
    {
        var p = Partida.Actual;
        if (r.ganado)
        {
            r.recompensa.monedas = 10;
            var materiales = new[] { Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso };
            for (int i = 0; i < 2; i++) r.recompensa[materiales[UnityEngine.Random.Range(0, materiales.Length)]]++;
            p.puntuacion += 20;
        }
        else
        {
            r.recompensa.monedas = 3;
            p.puntuacion = Mathf.Max(0, p.puntuacion - 15);
        }
        foreach (var rec in DatosInventario.Todos) p.inventario[rec] += r.recompensa[rec];
        r.puntosDespues = p.puntuacion;
        Partida.Cambiado();
        return r;
    }

    /// Vuelta al pueblo junto al cofre PvP.
    public void Volver()
    {
        Viaje.llegarA = "asalto";
        Transicion.Ir("Pueblo");
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
}
