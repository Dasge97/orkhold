using UnityEngine;

/// Enemigo básico: espera cerca de su sitio, persigue al jugador si se acerca, le pega si lo alcanza.
[RequireComponent(typeof(CharacterController), typeof(Vida))]
public class Enemigo : MonoBehaviour
{
    public float distanciaVer = 5f;
    public float distanciaPegar = 1.0f;
    public float velocidad = 2f;
    public float cadenciaAtaque = 1.3f;
    public int dano = 1;
    public Animator animador;
    public GameObject sueltaAlMorir;    // prefab o modelo que deja en el suelo (moneda)

    public Vida Vida { get; private set; }
    CharacterController _cc;
    Transform _jugador;
    Vida _vidaJugador;
    float _proximoAtaque;
    float _ocupadoHasta;
    string _estado = "";

    void Awake()
    {
        Vida = GetComponent<Vida>();
        _cc = GetComponent<CharacterController>();
        if (!animador) animador = GetComponentInChildren<Animator>();
        Vida.AlMorir += Morir;
    }

    void Start()
    {
        var j = FindAnyObjectByType<ControlJugador>();
        if (j) { _jugador = j.transform; _vidaJugador = j.GetComponent<Vida>(); }
    }

    void Update()
    {
        if (Vida.Muerto || !_jugador || Time.time < _ocupadoHasta) return;
        if (_vidaJugador && _vidaJugador.Muerto) { Animar("idle"); return; }

        Vector3 aJugador = _jugador.position - transform.position; aJugador.y = 0f;
        float d = aJugador.magnitude;

        if (d <= distanciaPegar)
        {
            Mirar(aJugador);
            if (Time.time >= _proximoAtaque)
            {
                _proximoAtaque = Time.time + cadenciaAtaque;
                _ocupadoHasta = Time.time + 0.5f;
                Animar("attack-melee-right", true);
                Invoke(nameof(Golpear), 0.25f);
            }
            else Animar("idle");
        }
        else if (d <= distanciaVer)
        {
            Mirar(aJugador);
            _cc.Move((aJugador.normalized * velocidad + Vector3.down * 2f) * Time.deltaTime);
            Animar("walk");
        }
        else
        {
            _cc.Move(Vector3.down * 2f * Time.deltaTime);
            Animar("idle");
        }
    }

    void Golpear()
    {
        if (!_jugador || Vida.Muerto) return;
        if (Vector3.Distance(_jugador.position, transform.position) <= distanciaPegar + 0.3f)
            _vidaJugador?.Danar(dano);
    }

    public void RecibirGolpe(int cantidad, ControlJugador quien)
    {
        Vida.Danar(cantidad);
        if (Vida.Muerto) { quien?.RegistrarBaja(); return; }
        _ocupadoHasta = Time.time + 0.3f;
        // Empujón hacia atrás al recibir el golpe.
        Vector3 atras = (transform.position - quien.transform.position); atras.y = 0f;
        _cc.Move(atras.normalized * 0.4f);
    }

    void Morir()
    {
        Animar("die", true);
        _cc.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (sueltaAlMorir)
        {
            var g = Instantiate(sueltaAlMorir, transform.position + Vector3.up * 0.2f, Quaternion.identity);
            g.SetActive(true);
        }
        Destroy(gameObject, 2.5f);
    }

    void Mirar(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
    }

    void Animar(string estado, bool forzar = false)
    {
        if (!animador) return;
        if (!forzar && estado == _estado) return;
        _estado = estado;
        animador.CrossFadeInFixedTime(estado, 0.1f, 0, 0f);
    }
}
