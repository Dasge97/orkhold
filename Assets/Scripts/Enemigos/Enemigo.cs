using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Enemigo base (01-diseno.md §4.2). Estados: quieto, patrulla (entre su sitio y un segundo punto),
/// persigue, ataca, vuelve a su sitio y muerto. Al morir suelta su botín y reaparece en su sitio
/// a los 60 s. El orco y el esqueleto son esta clase con otros números; el arquero y el jefe la amplían.
[RequireComponent(typeof(CharacterController), typeof(Vida))]
public class Enemigo : MonoBehaviour, IGolpeable
{
    public enum Estado { Quieto, Patrulla, Persigue, Ataca, Vuelve, Muerto }

    [Header("Números")]
    public string nombre = "Orco";
    public float distanciaVer = 5f;
    public float distanciaPegar = 1.0f;
    public float velocidad = 2f;
    public float cadenciaAtaque = 1.3f;
    public int dano = 1;
    [Tooltip("Si se aleja más que esto de su sitio, deja de perseguir y vuelve.")]
    public float distanciaPerder = 9f;
    [Tooltip("Segundos hasta reaparecer tras morir. 0 = no reaparece.")]
    public float reaparecerSegundos = 60f;

    [Header("Patrulla")]
    public bool patrulla;
    [Tooltip("Segundo punto de la patrulla (el primero es su sitio).")]
    public Vector3 puntoPatrulla;

    [Header("Botín")]
    public int monedasMin = 1;
    public int monedasMax = 2;
    public Recurso recursoExtra = Recurso.Hierro;
    [Range(0f, 1f)] public float probabilidadExtra;

    public Animator animador;

    public Vida Vida { get; private set; }
    public Estado EstadoActual { get; protected set; }
    public bool PuedeRecibirGolpe => Vida && !Vida.Muerto;
    public Transform Transform => transform;
    public Vector3 Casa => _casa;
    /// Animación actual y si se ve (EnemigoRed los copia a los clientes).
    public string EstadoAnim => _anim;
    public bool Visible => _renders == null || _renders.Length == 0 || !_renders[0] || _renders[0].enabled;

    public static readonly List<Enemigo> Todos = new List<Enemigo>();
    /// Cualquier enemigo que muere (para el jefe, los tests y el servidor).
    public static event Action<Enemigo> AlMorirEnemigo;

    protected CharacterController _cc;
    protected Transform _jugador;
    protected Vida _vidaJugador;
    protected float _proximoAtaque;
    protected float _ocupadoHasta;
    Vector3 _casa;
    Quaternion _giroCasa;
    float _esperaHasta;
    bool _haciaPunto = true;
    string _anim = "";
    Renderer[] _renders;
    Collider[] _colliders;

    protected virtual void Awake()
    {
        Vida = GetComponent<Vida>();
        _cc = GetComponent<CharacterController>();
        if (!animador) animador = GetComponentInChildren<Animator>();
        _renders = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        Vida.AlMorir += Morir;
    }

    protected virtual void OnEnable() { Todos.Add(this); }
    protected virtual void OnDisable() { Todos.Remove(this); }

    protected virtual void Start()
    {
        // En el pueblo, el tiempo de reaparecer sale del balance (los defensores del fuerte no reaparecen).
        if (reaparecerSegundos > 0f && GestorEscena.Actual && GestorEscena.Actual.esPueblo && Balance.Cargado)
            reaparecerSegundos = Balance.Real("mundo.enemigos_reaparecer", reaparecerSegundos);
        // Mazmorras 2-4: más vida (11-mazmorras.md §5). El jefe pone la suya aparte.
        var g = GestorEscena.Actual;
        if (g && g.esMazmorra && g.numeroMazmorra > 1 && !(this is Jefe))
        {
            float mult = Balance.Real($"mazmorra{g.numeroMazmorra}.multiplicador_vida", MultiplicadorPorDefecto(g.numeroMazmorra));
            Vida.maxima = Mathf.Max(1, Mathf.RoundToInt(Vida.maxima * mult));
            Vida.actual = Vida.maxima;
        }
        // Nombre en rojo y barra de vida al acercarse o al pegarle (15-interfaz.md §4). Los jefes llevan la barra grande.
        if (!(this is Jefe jefe && jefe.deMazmorra)) Rotulo.Poner(gameObject, Rotulo.Tipo.Enemigo, nombre);
        _casa = transform.position;
        _giroCasa = transform.rotation;
        EstadoActual = patrulla ? Estado.Patrulla : Estado.Quieto;
        BuscarJugador();
    }

    public static float MultiplicadorPorDefecto(int mazmorra) => mazmorra == 2 ? 1.3f : mazmorra == 3 ? 1.5f : mazmorra == 4 ? 1.8f : 1f;

    /// El jugador vivo más cercano (en el pueblo online hay varios).
    void BuscarJugador()
    {
        ControlJugador mejor = null; float dMejor = float.MaxValue;
        foreach (var j in ControlJugador.Todos)
        {
            if (!j || j.Vida.Muerto) continue;
            float d = (j.transform.position - transform.position).sqrMagnitude;
            if (d < dMejor) { dMejor = d; mejor = j; }
        }
        if (mejor) { _jugador = mejor.transform; _vidaJugador = mejor.Vida; }
        else { _jugador = null; _vidaJugador = null; }
    }

    float _siguienteBusqueda;

    protected virtual void Update()
    {
        if (EstadoActual == Estado.Muerto) return;
        if (!_jugador || Time.time >= _siguienteBusqueda) { _siguienteBusqueda = Time.time + 0.5f; BuscarJugador(); }
        if (!_jugador) { Caer(); Animar("idle"); return; }
        if (Time.time < _ocupadoHasta) { Caer(); return; }

        Vector3 aJugador = _jugador.position - transform.position; aJugador.y = 0f;
        float d = aJugador.magnitude;
        bool jugadorVivo = _vidaJugador && !_vidaJugador.Muerto;
        Pensar(jugadorVivo ? d : float.MaxValue, aJugador);
    }

    /// Decide qué hacer. "d" es la distancia al jugador (infinita si está muerto).
    protected virtual void Pensar(float d, Vector3 aJugador)
    {
        float lejosDeCasa = Plano(transform.position - _casa).magnitude;
        switch (EstadoActual)
        {
            case Estado.Quieto:
            case Estado.Patrulla:
                if (d <= distanciaVer) { EstadoActual = Estado.Persigue; break; }
                if (EstadoActual == Estado.Patrulla) Patrullar(); else { Caer(); Animar("idle"); }
                break;
            case Estado.Persigue:
                if (d <= distanciaPegar) { EstadoActual = Estado.Ataca; break; }
                if (d > distanciaVer * 1.6f || lejosDeCasa > distanciaPerder) { EstadoActual = Estado.Vuelve; break; }
                Mirar(aJugador);
                Mover(aJugador, velocidad);
                break;
            case Estado.Ataca:
                if (d > distanciaPegar + 0.25f) { EstadoActual = Estado.Persigue; break; }
                Mirar(aJugador);
                Caer();
                if (Time.time >= _proximoAtaque) Atacar();
                else Animar("idle");
                break;
            case Estado.Vuelve:
                if (d <= distanciaVer * 0.7f && lejosDeCasa < distanciaPerder * 0.8f) { EstadoActual = Estado.Persigue; break; }
                Vector3 aCasa = Plano(_casa - transform.position);
                if (aCasa.magnitude < 0.25f)
                {
                    transform.rotation = _giroCasa;
                    EstadoActual = patrulla ? Estado.Patrulla : Estado.Quieto;
                    Animar("idle");
                    break;
                }
                Mirar(aCasa);
                Mover(aCasa, velocidad * 0.8f);
                break;
        }
    }

    void Patrullar()
    {
        if (Time.time < _esperaHasta) { Caer(); Animar("idle"); return; }
        Vector3 destino = _haciaPunto ? puntoPatrulla : _casa;
        Vector3 a = Plano(destino - transform.position);
        if (a.magnitude < 0.25f)
        {
            _haciaPunto = !_haciaPunto;
            _esperaHasta = Time.time + 1.5f;
            return;
        }
        Mirar(a);
        Mover(a, velocidad * 0.5f);
    }

    /// Ataque cuerpo a cuerpo: animación y golpe 0,25 s después si el jugador sigue cerca.
    protected virtual void Atacar()
    {
        _proximoAtaque = Time.time + cadenciaAtaque;
        _ocupadoHasta = Time.time + 0.5f;
        Animar("attack-melee-right", true);
        Invoke(nameof(Golpear), 0.25f);
    }

    void Golpear()
    {
        if (!_jugador || Vida.Muerto || !_vidaJugador) return;
        if (Plano(_jugador.position - transform.position).magnitude <= distanciaPegar + 0.35f)
            _vidaJugador.Danar(dano, transform.position);
    }

    public virtual void RecibirGolpe(int cantidad, Vector3 desde, ControlJugador quien)
    {
        Vida.Danar(cantidad, desde);
        Sonidos.Tocar("golpe", transform.position);
        if (Vida.Muerto) { if (quien) quien.RegistrarBaja(); return; }
        _ocupadoHasta = Time.time + 0.3f;
        if (EstadoActual == Estado.Quieto || EstadoActual == Estado.Patrulla || EstadoActual == Estado.Vuelve)
            EstadoActual = Estado.Persigue;
        Vector3 atras = Plano(transform.position - desde);
        if (_cc.enabled) _cc.Move(atras.normalized * 0.4f);
    }

    protected virtual void Morir()
    {
        EstadoActual = Estado.Muerto;
        CancelInvoke();
        Animar("die", true);
        Sonidos.Tocar("morir", transform.position, 0.6f);
        _cc.enabled = false;
        foreach (var c in _colliders) if (c) c.enabled = false;
        SoltarBotin();
        AlMorirEnemigo?.Invoke(this);
        StartCoroutine(DesaparecerYVolver());
    }

    protected virtual void SoltarBotin()
    {
        int n = UnityEngine.Random.Range(monedasMin, monedasMax + 1);
        if (n > 0) Catalogo.Soltar(Recurso.Moneda, transform.position + Vector3.up * 0.05f, n);
        if (probabilidadExtra > 0f && UnityEngine.Random.value < probabilidadExtra)
            Catalogo.Soltar(recursoExtra, transform.position + transform.right * 0.4f + Vector3.up * 0.05f);
    }

    IEnumerator DesaparecerYVolver()
    {
        yield return new WaitForSeconds(2.5f);
        foreach (var r in _renders) if (r) r.enabled = false;
        if (reaparecerSegundos <= 0f) yield break;
        yield return new WaitForSeconds(Mathf.Max(0f, reaparecerSegundos - 2.5f));
        Reaparecer();
    }

    /// Vuelve a su sitio con la vida llena.
    public void Reaparecer()
    {
        StopAllCoroutines();
        transform.SetPositionAndRotation(_casa, _giroCasa);
        Vida.Revivir();
        _cc.enabled = true;
        foreach (var c in _colliders) if (c) c.enabled = true;
        foreach (var r in _renders) if (r) r.enabled = true;
        EstadoActual = patrulla ? Estado.Patrulla : Estado.Quieto;
        _haciaPunto = true;
        Animar("idle", true);
    }

    /// Copias en los clientes del pueblo online: animación y visibilidad que dice el servidor.
    public void AnimarDesdeRed(string estado) => Animar(estado);
    public void MostrarDesdeRed(bool si) { foreach (var r in _renders) if (r) r.enabled = si; }

    // ---------- ayudas ----------

    protected void Mover(Vector3 direccion, float vel)
    {
        if (!_cc.enabled) return;
        _cc.Move((direccion.normalized * vel + Vector3.down * 2f) * Time.deltaTime);
        Animar("walk");
    }

    protected void Caer()
    {
        if (_cc.enabled) _cc.Move(Vector3.down * 2f * Time.deltaTime);
    }

    protected void Mirar(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(Plano(dir)), 540f * Time.deltaTime);
    }

    protected static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    protected void Animar(string estado, bool forzar = false)
    {
        if (!animador) return;
        if (!forzar && estado == _anim) return;
        _anim = estado;
        animador.CrossFadeInFixedTime(estado, 0.1f, 0, 0f);
    }
}
