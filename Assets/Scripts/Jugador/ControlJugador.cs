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
    public float alcanceRecoger = 1.3f;
    public Animator animador;
    public Transform cameraRef;

    CharacterController _cc;
    Vida _vida;
    Inventario _inv;
    float _vy;
    float _ocupadoHasta;
    string _estado = "";

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _vida = GetComponent<Vida>();
        _inv = GetComponent<Inventario>();
        if (!animador) animador = GetComponentInChildren<Animator>();
        _vida.AlMorir += Morir;
    }

    void Update()
    {
        if (_vida.Muerto) return;

        bool ocupado = Time.time < _ocupadoHasta;
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
        _cc.Move((dir * vel + Vector3.up * _vy) * Time.deltaTime);

        if (!ocupado)
        {
            if (EntradaJuego.Ataque) { Atacar(); return; }
            if (EntradaJuego.Interactuar) { Interactuar(); return; }
            Animar(entrada.magnitude < 0.1f ? "idle" : corre ? "sprint" : "walk");
        }
    }

    void LateUpdate() { EntradaJuego.LimpiarPulsos(); }

    void Atacar()
    {
        _ocupadoHasta = Time.time + duracionAtaque;
        Animar("attack-melee-right", true);
        Invoke(nameof(AplicarGolpe), duracionAtaque * 0.4f);
    }

    void AplicarGolpe()
    {
        Vector3 centro = transform.position + transform.forward * alcanceAtaque * 0.6f + Vector3.up * 0.4f;
        foreach (var c in Physics.OverlapSphere(centro, alcanceAtaque))
        {
            var e = c.GetComponentInParent<Enemigo>();
            if (e && !e.Vida.Muerto) e.RecibirGolpe(danoAtaque, this);
        }
    }

    void Interactuar()
    {
        Recogible mejor = null; float dMin = alcanceRecoger;
        foreach (var r in FindObjectsByType<Recogible>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(r.transform.position, transform.position);
            if (d < dMin) { dMin = d; mejor = r; }
        }
        if (mejor)
        {
            _ocupadoHasta = Time.time + 0.7f;
            Animar("pick-up", true);
            _inv.Sumar(mejor.clase);
            Destroy(mejor.gameObject);
        }
        else if (_vida.actual < _vida.maxima && _inv.UsarPocion())
        {
            _vida.Curar(2);
        }
    }

    public void RegistrarBaja() { _inv.SumarBaja(); }

    void Morir()
    {
        Animar("die", true);
        Invoke(nameof(Reiniciar), 2.5f);
    }

    void Reiniciar() { SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

    void Animar(string estado, bool forzar = false)
    {
        if (!animador) return;
        if (!forzar && estado == _estado) return;
        _estado = estado;
        animador.CrossFadeInFixedTime(estado, 0.1f, 0, 0f);
    }
}
