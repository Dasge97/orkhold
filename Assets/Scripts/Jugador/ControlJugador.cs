using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Movimiento, ataque e interacción del jugador. Las animaciones se eligen por nombre de estado
/// (los nombres son los clips que trae el FBX de Kenney: idle, walk, sprint, attack-melee-right, pick-up, die).
[RequireComponent(typeof(CharacterController))]
public class ControlJugador : MonoBehaviour
{
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

    /// Momento (Time.time) del último golpe dado o recibido.
    public float UltimoCombate { get; private set; } = -999f;
    public Vida Vida => _vida;

    CharacterController _cc;
    Vida _vida;
    Inventario _inv;
    float _vy;
    float _ocupadoHasta;
    string _estado = "";
    Vector3 _empuje;
    float _siguientePaso;

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
            Sonidos.Tocar("dano");
            Empujar(desde, 3f);
        };
    }

    void Update()
    {
        if (_vida.Muerto) { EntradaJuego.LimpiarPulsos(); return; }

        // Los pulsos de botón se leen y se borran aquí, para no perder ninguno por el orden de ejecución.
        bool pulsoAtaque = EntradaJuego.Ataque, pulsoInteractuar = EntradaJuego.Interactuar;
        EntradaJuego.LimpiarPulsos();

        bool ocupado = Time.time < _ocupadoHasta || Transicion.Ocupada;
        Vector2 entrada = ocupado ? Vector2.zero : EntradaJuego.Mover;

        // Dirección relativa a la cámara, aplanada sobre el suelo.
        Vector3 adelante = cameraRef ? Vector3.ProjectOnPlane(cameraRef.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        Vector3 dir = adelante * entrada.y + derecha * entrada.x;

        bool corre = EntradaJuego.Correr && entrada.magnitude > 0.5f;
        float vel = corre ? velocidadCorrer : velocidadAndar;

        if (dir.sqrMagnitude > 0.001f)
        {
            var rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, velocidadGiro * Time.deltaTime);
        }

        _vy = _cc.isGrounded ? -1f : _vy - 20f * Time.deltaTime;
        _cc.Move((dir * vel + Vector3.up * _vy + _empuje) * Time.deltaTime);
        _empuje = Vector3.MoveTowards(_empuje, Vector3.zero, 14f * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f && _cc.isGrounded && Time.time >= _siguientePaso)
        {
            _siguientePaso = Time.time + (corre ? 0.27f : 0.38f);
            var gestor = GestorEscena.Actual;
            Sonidos.Tocar(gestor && gestor.suelo == GestorEscena.Suelo.Piedra ? "pasos_piedra" : "pasos_hierba", null, 0.35f);
        }

        if (!ocupado)
        {
            if (pulsoAtaque) { Atacar(); return; }
            if (pulsoInteractuar) { Interactuar(); return; }
            Animar(entrada.magnitude < 0.1f ? "idle" : corre ? "sprint" : "walk");
        }
    }

    void Atacar()
    {
        Apuntar();
        _ocupadoHasta = Time.time + duracionAtaque;
        Animar("attack-melee-right", true);
        Invoke(nameof(AplicarGolpe), duracionAtaque * 0.4f);
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
        foreach (var c in Physics.OverlapSphere(centro, alcanceAtaque))
        {
            var g = c.GetComponentInParent<IGolpeable>();
            if (g == null || !g.PuedeRecibirGolpe || ReferenceEquals(g.Transform, transform) || !golpeados.Add(g)) continue;
            g.RecibirGolpe(danoAtaque, transform.position, this);
            UltimoCombate = Time.time;
        }
    }

    void Interactuar()
    {
        var cerca = Interactuable.MasCercano(transform.position);
        if (cerca)
        {
            Vector3 a = cerca.transform.position - transform.position; a.y = 0f;
            if (a.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(a);
            cerca.Usar(this);
        }
        else if (_vida.actual < _vida.maxima && _inv.UsarPocion())
        {
            _vida.Curar(2);
        }
    }

    /// Lo llama un Recogible al usarlo: animación de recoger y suma al inventario.
    public void Recoger(Recogible r)
    {
        _ocupadoHasta = Time.time + 0.7f;
        Animar("pick-up", true);
        _inv.Sumar(r.recurso, r.cantidad);
        Sonidos.Tocar(r.recurso == Recurso.Moneda ? "recoger_moneda" : "recoger");
        NumeroFlotante.Crear(transform.position + Vector3.up * 1.1f, $"+{r.cantidad} {Nombres.De(r.recurso)}", new Color(1f, 0.9f, 0.4f), 0.8f);
    }

    public void RegistrarBaja() { _inv.SumarBaja(); }

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
        Sonidos.Tocar("morir");
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
        _vida.Revivir();
        _empuje = Vector3.zero;
        _ocupadoHasta = 0f;
        GestorEscena.Colocar(transform, donde, giro);
        Animar("idle", true);
    }

    void Animar(string estado, bool forzar = false)
    {
        if (!animador) return;
        if (!forzar && estado == _estado) return;
        _estado = estado;
        animador.CrossFadeInFixedTime(estado, 0.1f, 0, 0f);
    }
}
