using UnityEngine;

/// Trampa de pinchos (01-diseno.md §4.3, §5.1). En la mazmorra los pinchos suben cada 2 s y hacen
/// 1 de daño a quien esté encima. En el fuerte ("oculta") no se ven hasta que alguien la pisa.
public class Trampa : MonoBehaviour
{
    public float periodo = 2f;
    public float arriba = 0.6f;
    public int dano = 1;
    [Tooltip("Mitad del lado de la zona que hace daño.")]
    public float radio = 0.45f;
    [Tooltip("Oculta hasta que alguien la pisa (trampa del fuerte).")]
    public bool oculta;
    public Animator animador;

    ControlJugador _jugador;
    float _t;
    bool _subida, _danoHecho;
    Renderer[] _renders;

    public bool Subida => _subida;

    void Start()
    {
        if (!animador) animador = GetComponentInChildren<Animator>();
        _renders = GetComponentsInChildren<Renderer>();
        // Desfase según la posición para que no suban todas a la vez.
        _t = Mathf.Repeat(transform.position.x * 0.37f + transform.position.z * 0.61f, periodo);
        if (oculta) foreach (var r in _renders) r.enabled = false;
        Poner(false);
    }

    void Update()
    {
        if (!_jugador) { _jugador = FindAnyObjectByType<ControlJugador>(); if (!_jugador) return; }
        Vector3 d = _jugador.transform.position - transform.position;
        bool encima = Mathf.Abs(d.x) <= radio && Mathf.Abs(d.z) <= radio && Mathf.Abs(d.y) < 0.6f;

        if (oculta)
        {
            // En el fuerte: se descubre y pincha al pisarla, como mucho cada "periodo".
            _t += Time.deltaTime;
            if (encima && _t >= periodo)
            {
                _t = 0f;
                foreach (var r in _renders) r.enabled = true;
                Poner(true);
                _jugador.Vida.Danar(dano, transform.position);
            }
            else if (_subida && _t > arriba) Poner(false);
            return;
        }

        _t += Time.deltaTime;
        if (_t >= periodo) { _t -= periodo; _danoHecho = false; }
        bool tocaSubir = _t < arriba;
        if (tocaSubir != _subida) Poner(tocaSubir);
        if (_subida && encima && !_danoHecho)
        {
            _danoHecho = true;
            _jugador.Vida.Danar(dano, transform.position);
        }
    }

    void Poner(bool subir)
    {
        _subida = subir;
        if (animador && animador.runtimeAnimatorController) animador.CrossFadeInFixedTime(subir ? "show" : "hide", 0.05f, 0, 0f);
    }
}
