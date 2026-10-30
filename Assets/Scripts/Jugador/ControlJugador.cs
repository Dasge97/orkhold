using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Movimiento, ataque e interacción del jugador. Las animaciones se eligen por nombre de estado
/// (los nombres son los clips que trae el FBX de Kenney: idle, walk, sprint, attack-melee-right, pick-up, die).
///
/// En el pueblo online (fase 7) hay varias copias: el servidor mueve a todos con la entrada que le manda
/// cada cliente ("leerEntrada") y decide daño y recogidas ("TieneAutoridad"); el dueño predice su propio
/// movimiento; las copias de los demás solo se dibujan (las mueve JugadorRed).
[RequireComponent(typeof(CharacterController))]
public class ControlJugador : MonoBehaviour
{
    /// Lo que el jugador pide en un fotograma.
    public struct Entrada
    {
        public Vector2 mover;
        public bool correr, ataque, interactuar;
    }

    public float velocidadAndar = 2.5f;
    public float velocidadCorrer = 4.5f;
    public float velocidadGiro = 720f;
    public float alcanceAtaque = 0.9f;
    public int danoAtaque = 1;
    public float duracionAtaque = 0.6f;
    [Tooltip("Al atacar, se gira solo hacia lo golpeable más cercano dentro de esta distancia.")]
    public float alcanceAutoApuntar = 1.6f;
    public Animator animador;
    public Transform cameraRef;

    /// El jugador que maneja este móvil (en el pueblo online hay más).
    public static ControlJugador Local { get; private set; }
    /// Todos los jugadores de la escena (en el servidor, los de todos los clientes).
    public static readonly List<ControlJugador> Todos = new List<ControlJugador>();

    /// De dónde sale la entrada. Por defecto, los controles de la pantalla (EntradaJuego).
    public Func<Entrada> leerEntrada = LeerEntradaLocal;
    /// Decide daño, recogidas y muerte (falso en la copia del dueño cuando hay un servidor aparte).
    public bool TieneAutoridad { get; set; } = true;
    /// Se mueve con su CharacterController (falso en las copias de otros jugadores en un cliente).
    public bool Simula { get; set; } = true;
    /// Último estado de animación (JugadorRed lo manda a los demás).
    public string EstadoAnim => _estado;

    /// Momento (Time.time) del último golpe dado o recibido.
    public float UltimoCombate { get; private set; } = -999f;
    public Vida Vida => _vida;
    /// Al recoger algo con autoridad en el pueblo online (el servidor lo manda a la API y avisa al dueño).
    public event Action<Recurso, int> AlRecoger;

    CharacterController _cc;
    Vida _vida;
    Inventario _inv;
    float _vy;
    float _ocupadoHasta;
    string _estado = "";
    Vector3 _empuje;
    float _siguientePaso;

    public static Entrada LeerEntradaLocal()
    {
        var e = new Entrada { mover = EntradaJuego.Mover, correr = EntradaJuego.Correr, ataque = EntradaJuego.Ataque, interactuar = EntradaJuego.Interactuar };
        EntradaJuego.LimpiarPulsos();
        return e;
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _vida = GetComponent<Vida>();
        _inv = GetComponent<Inventario>();
        if (!animador) animador = GetComponentInChildren<Animator>();
        _vida.AlMorir += Morir;
        _vida.AlRecibirDano += (n, desde) =>
        {
            UltimoCombate = Time.time;
            if (this == Local) { Sonidos.Tocar("dano"); Ajustes.Vibrar(); }
            if (Simula) Empujar(desde, 3f);
        };
        // Sin red, el único jugador es el local. En el pueblo online lo marca JugadorRed (solo el del dueño).
        if (!GetComponent<Unity.Netcode.NetworkObject>()) Local = this;
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); if (Local == this) Local = null; }

    /// JugadorRed marca cuál es el jugador de este móvil.
    public static void MarcarLocal(ControlJugador j) { Local = j; }

    void Update()
    {
        if (!Simula) return;
        var entrada = leerEntrada();
        if (_vida.Muerto) return;

        bool ocupado = Time.time < _ocupadoHasta || (this == Local && Transicion.Ocupada);
        Vector2 mover = ocupado ? Vector2.zero : entrada.mover;

        // Dirección relativa a la cámara, aplanada sobre el suelo (sin cámara, la de juego mira al norte).
        Vector3 adelante = cameraRef ? Vector3.ProjectOnPlane(cameraRef.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        Vector3 dir = adelante * mover.y + derecha * mover.x;

        bool corre = entrada.correr && mover.magnitude > 0.5f;
        float vel = corre ? velocidadCorrer : velocidadAndar;

        if (dir.sqrMagnitude > 0.001f)
        {
            var rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, velocidadGiro * Time.deltaTime);
        }

        _vy = _cc.isGrounded ? -1f : _vy - 20f * Time.deltaTime;
        _cc.Move((dir * vel + Vector3.up * _vy + _empuje) * Time.deltaTime);
        _empuje = Vector3.MoveTowards(_empuje, Vector3.zero, 14f * Time.deltaTime);

        if (this == Local && dir.sqrMagnitude > 0.01f && _cc.isGrounded && Time.time >= _siguientePaso)
        {
            _siguientePaso = Time.time + (corre ? 0.27f : 0.38f);
            var gestor = GestorEscena.Actual;
            Sonidos.Tocar(gestor && gestor.suelo == GestorEscena.Suelo.Piedra ? "pasos_piedra" : "pasos_hierba", null, 0.35f);
        }

        if (!ocupado)
        {
            if (entrada.ataque) { Atacar(); return; }
            if (entrada.interactuar) { Interactuar(); return; }
            Animar(mover.magnitude < 0.1f ? "idle" : corre ? "sprint" : "walk");
        }
    }

    void Atacar()
    {
        Apuntar();
        _ocupadoHasta = Time.time + duracionAtaque;
        Animar("attack-melee-right", true);
        if (TieneAutoridad) Invoke(nameof(AplicarGolpe), duracionAtaque * 0.4f);
    }

    /// Se gira hacia lo golpeable más cercano: en el móvil es difícil apuntar con el joystick.
    void Apuntar()
    {
        IGolpeable mejor = null; float dMejor = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(transform.position + Vector3.up * 0.4f, alcanceAutoApuntar))
        {
            var g = c.GetComponentInParent<IGolpeable>();
            if (g == null || !g.PuedeRecibirGolpe || ReferenceEquals(g.Transform, transform)) continue;
            Vector3 d = g.Transform.position - transform.position; d.y = 0f;
            if (d.magnitude < dMejor) { dMejor = d.magnitude; mejor = g; }
        }
        if (mejor == null) return;
        Vector3 a = mejor.Transform.position - transform.position; a.y = 0f;
        if (a.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(a);
    }

    void AplicarGolpe()
    {
        if (_vida.Muerto) return;
        Vector3 centro = transform.position + transform.forward * alcanceAtaque * 0.6f + Vector3.up * 0.4f;
        var golpeados = new HashSet<IGolpeable>();
        // Personajes: todos los que alcance el golpe. Piezas del fuerte: solo la más cercana (un muro por golpe).
        PiezaFuerte pieza = null; float dPieza = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(centro, alcanceAtaque))
        {
            var g = c.GetComponentInParent<IGolpeable>();
            if (g == null || !g.PuedeRecibirGolpe || ReferenceEquals(g.Transform, transform) || !golpeados.Add(g)) continue;
            if (g is PiezaFuerte pf)
            {
                float d = Vector3.Distance(pf.transform.position + Vector3.up * 0.4f, centro);
                if (d < dPieza) { dPieza = d; pieza = pf; }
                continue;
            }
            g.RecibirGolpe(danoAtaque, transform.position, this);
            UltimoCombate = Time.time;
        }
        if (pieza) pieza.RecibirGolpe(danoAtaque, transform.position, this);
    }

    /// Lo de este lado: el dueño usa puertas, cofre PvP y tablón; quien tiene autoridad, los recogibles.
    void Interactuar()
    {
        var cerca = Interactuable.MasCercano(transform.position, this == Local, TieneAutoridad);
        if (cerca)
        {
            Vector3 a = cerca.transform.position - transform.position; a.y = 0f;
            if (a.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(a);
            cerca.Usar(this);
        }
        else if (TieneAutoridad && this == Local && _vida.actual < _vida.maxima && _inv && _inv.UsarPocion())
        {
            _vida.Curar(2);
        }
    }

    /// Lo llama un Recogible al usarlo: animación de recoger y suma al inventario.
    public void Recoger(Recogible r)
    {
        _ocupadoHasta = Time.time + 0.7f;
        Animar("pick-up", true);
        if (AlRecoger != null) { AlRecoger(r.recurso, r.cantidad); return; }   // pueblo online: lo lleva el servidor
        _inv.Sumar(r.recurso, r.cantidad);
        var g = GestorEscena.Actual;
        if (g && g.esPueblo) Sincronizacion.AnotarPueblo(r.recurso, r.cantidad);
        MostrarRecogido(r.recurso, r.cantidad);
    }

    /// Sonido y número de lo recogido (en el pueblo online lo llama el aviso del servidor).
    public void MostrarRecogido(Recurso r, int n)
    {
        Sonidos.Tocar(r == Recurso.Moneda ? "recoger_moneda" : "recoger");
        NumeroFlotante.Crear(transform.position + Vector3.up * 1.1f, $"+{n} {Nombres.De(r)}", new Color(1f, 0.9f, 0.4f), 0.8f);
    }

    public void RegistrarBaja() { if (_inv) _inv.SumarBaja(); }

    /// Empujón en dirección contraria a "desde".
    public void Empujar(Vector3 desde, float fuerza)
    {
        Vector3 d = transform.position - desde; d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) d = -transform.forward;
        _empuje = d.normalized * fuerza;
    }

    void Morir()
    {
        Animar("die", true);
        if (this == Local) Sonidos.Tocar("morir");
        CancelInvoke(nameof(AplicarGolpe));
        Invoke(nameof(AvisarMuerte), 1.2f);
    }

    void AvisarMuerte()
    {
        if (GestorEscena.Actual) GestorEscena.Actual.JugadorMuerto(this);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// Vuelve a la vida en un sitio (reaparición en la fogata).
    public void Revivir(Vector3 donde, float giro)
    {
        if (TieneAutoridad) _vida.Revivir();
        _empuje = Vector3.zero;
        _ocupadoHasta = 0f;
        if (Simula) GestorEscena.Colocar(transform, donde, giro, this == Local);
        Animar("idle", true);
    }

    /// Pone una animación por nombre (también la usa JugadorRed en las copias de otros jugadores).
    public void Animar(string estado, bool forzar = false)
    {
        if (!animador) return;
        if (!forzar && estado == _estado) return;
        _estado = estado;
        animador.CrossFadeInFixedTime(estado, 0.1f, 0, 0f);
    }
}
