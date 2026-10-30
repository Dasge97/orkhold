using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// Jugador en el pueblo online (tarea 7.2 y 7.3), con autoridad del servidor:
/// - El dueño manda su entrada al servidor y predice su propio movimiento; si se separa de lo que dice
///   el servidor, se corrige poco a poco (o de golpe si la diferencia es grande).
/// - El servidor mueve al jugador con esa entrada y decide daño, vida y recogidas.
/// - Los demás clientes lo dibujan interpolando posición y giro, con su animación, modelo y nombre encima.
[DefaultExecutionOrder(-20)]
[RequireComponent(typeof(ControlJugador), typeof(NetworkObject))]
public class JugadorRed : NetworkBehaviour
{
    static readonly string[] Animaciones = { "idle", "walk", "sprint", "attack-melee-right", "pick-up", "die" };
    const float EnvioCada = 1f / 30f;
    const float CorregirDesde = 0.35f;
    const float SaltarDesde = 3f;

    readonly NetworkVariable<Vector3> _pos = new NetworkVariable<Vector3>();
    readonly NetworkVariable<float> _giro = new NetworkVariable<float>();
    readonly NetworkVariable<byte> _anim = new NetworkVariable<byte>();
    readonly NetworkVariable<int> _vida = new NetworkVariable<int>(6);
    readonly NetworkVariable<FixedString64Bytes> _nombre = new NetworkVariable<FixedString64Bytes>();
    readonly NetworkVariable<FixedString64Bytes> _modelo = new NetworkVariable<FixedString64Bytes>();

    ControlJugador _c;
    Vida _v;
    CharacterController _cc;
    ControlJugador.Entrada _remota;
    bool _pulsoAtaque, _pulsoInteractuar;
    float _siguienteEnvio;
    Vector2 _ultimoMover;
    bool _ultimoCorrer;

    public string Nombre => _nombre.Value.ToString();

    void Awake()
    {
        _c = GetComponent<ControlJugador>();
        _v = GetComponent<Vida>();
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        // NGO pone la posición después de crear el objeto: el CharacterController aún tiene la vieja
        // (el origen) y en su primer Move devolvería al jugador allí. Apagar y encender lo pone al día.
        if (_cc.enabled) { _cc.enabled = false; _cc.enabled = true; }
        if (IsServer)
        {
            _c.TieneAutoridad = true;
            _c.Simula = true;
            if (!IsOwner) _c.leerEntrada = LeerRemota;
            var red = RedPueblo.Actual;
            if (red && red.Clientes.TryGetValue(OwnerClientId, out var cli))
            {
                _nombre.Value = Recortar(cli.datos.nombre);
                _modelo.Value = Recortar(cli.datos.modelo);
            }
            _vida.Value = _v.actual;
            _v.AlCambiar += () => _vida.Value = _v.actual;
            // El dueño anfitrión recoge como siempre; para los clientes, el servidor avisa a la API y al móvil.
            if (!IsOwner) _c.AlRecoger += (r, n) => { RedPueblo.Actual?.ServidorRecogio(OwnerClientId, r, n); RecogidoRpc(r, n); };
        }
        else
        {
            _c.TieneAutoridad = false;
            _vida.OnValueChanged += (_, nueva) => _v.Establecer(nueva, transform.position - transform.forward);
            _v.Establecer(_vida.Value, transform.position);
        }

        if (IsOwner)
        {
            ControlJugador.MarcarLocal(_c);
            _c.Simula = true;
            var cam = FindAnyObjectByType<CamaraSigue>();
            if (cam) { cam.objetivo = transform; _c.cameraRef = cam.transform; cam.Saltar(); }
        }
        else if (!IsServer)
        {
            // Copia de otro jugador: solo se dibuja.
            _c.Simula = false;
            _cc.enabled = false;
            gameObject.AddComponent<NombreEncima>().jugador = this;
        }

        AplicarModelo();
        _modelo.OnValueChanged += (_, __) => AplicarModelo();
    }

    static FixedString64Bytes Recortar(string s)
    {
        s ??= "";
        return new FixedString64Bytes(s.Length > 30 ? s.Substring(0, 30) : s);
    }

    void AplicarModelo()
    {
        string m = _modelo.Value.ToString();
        if (IsOwner || string.IsNullOrEmpty(m)) return;   // el dueño ya lleva el suyo (Partida.modelo)
        var pj = GetComponent<PersonajeJugador>();
        if (pj && pj.Actual != m) pj.Poner(m);
    }

    void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner && !IsServer) EnviarEntrada();
        if (IsServer)
        {
            _pos.Value = transform.position;
            _giro.Value = transform.eulerAngles.y;
            int i = System.Array.IndexOf(Animaciones, _c.EstadoAnim);
            if (i >= 0) _anim.Value = (byte)i;
        }
        else if (!IsOwner) Interpolar();
    }

    void LateUpdate()
    {
        if (!IsSpawned || IsServer || !IsOwner) return;
        // Corrección de la predicción del dueño.
        Vector3 d = _pos.Value - transform.position;
        if (d.magnitude > SaltarDesde) GestorEscena.Colocar(transform, _pos.Value, transform.eulerAngles.y, false);
        else if (d.magnitude > CorregirDesde && _cc.enabled) _cc.Move(d * Mathf.Min(1f, 6f * Time.deltaTime));
    }

    void Interpolar()
    {
        transform.position = Vector3.Lerp(transform.position, _pos.Value, Mathf.Min(1f, 12f * Time.deltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, _giro.Value, 0f), Mathf.Min(1f, 12f * Time.deltaTime));
        string a = Animaciones[Mathf.Min(_anim.Value, Animaciones.Length - 1)];
        if (a != _c.EstadoAnim) _c.Animar(a, a == "attack-melee-right" || a == "pick-up");
    }

    /// El dueño: lo que pide en pantalla va al servidor (los toques en cuanto se dan; el joystick 30 veces por segundo).
    void EnviarEntrada()
    {
        var mover = EntradaJuego.Mover;
        bool correr = EntradaJuego.Correr;
        bool ataque = EntradaJuego.AtaquePantalla || Input.GetKeyDown(KeyCode.Space);
        bool inter = EntradaJuego.InteractuarPantalla || Input.GetKeyDown(KeyCode.E);
        bool cambia = (mover - _ultimoMover).sqrMagnitude > 0.0004f || correr != _ultimoCorrer;
        if (!ataque && !inter && !cambia && Time.unscaledTime < _siguienteEnvio) return;
        _siguienteEnvio = Time.unscaledTime + EnvioCada;
        _ultimoMover = mover; _ultimoCorrer = correr;
        EntradaRpc(mover, correr, ataque, inter);
    }

    [Rpc(SendTo.Server)]
    void EntradaRpc(Vector2 mover, bool correr, bool ataque, bool interactuar)
    {
        _remota.mover = Vector2.ClampMagnitude(mover, 1f);
        _remota.correr = correr;
        _pulsoAtaque |= ataque;
        _pulsoInteractuar |= interactuar;
    }

    ControlJugador.Entrada LeerRemota()
    {
        var e = _remota;
        e.ataque = _pulsoAtaque; e.interactuar = _pulsoInteractuar;
        _pulsoAtaque = _pulsoInteractuar = false;
        return e;
    }

    /// Al móvil del dueño: ha recogido algo (el servidor ya lo ha mandado a la API).
    [Rpc(SendTo.Owner)]
    void RecogidoRpc(Recurso r, int n)
    {
        Partida.Sumar(r, n);
        _c.MostrarRecogido(r, n);
    }
}
