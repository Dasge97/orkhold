using UnityEngine;

/// Lanza del arquero: va en línea recta, daña al jugador si le da y se rompe contra muros.
public class Proyectil : MonoBehaviour
{
    public float velocidad = 7f;
    public int dano = 1;
    public float vidaSegundos = 3f;
    public float radioImpacto = 0.4f;
    [HideInInspector] public Transform dueno;

    float _t;
    ControlJugador _jugador;

    void Start() { _jugador = FindAnyObjectByType<ControlJugador>(); }

    void Update()
    {
        float paso = velocidad * Time.deltaTime;
        _t += Time.deltaTime;
        if (_t > vidaSegundos) { Destroy(gameObject); return; }

        // ¿Le da al jugador en este tramo?
        if (_jugador && !_jugador.Vida.Muerto)
        {
            Vector3 aJ = _jugador.transform.position + Vector3.up * 0.4f - transform.position;
            Vector3 aJPlano = aJ; aJPlano.y = 0f;
            if (aJPlano.magnitude <= radioImpacto + paso && Vector3.Dot(aJPlano, transform.forward) > -0.2f)
            {
                _jugador.Vida.Danar(dano, transform.position - transform.forward);
                Destroy(gameObject);
                return;
            }
        }

        // ¿Choca contra algo que no sea un enemigo ni el jugador?
        if (Physics.Raycast(transform.position, transform.forward, out var hit, paso, ~0, QueryTriggerInteraction.Ignore))
        {
            bool esEnemigo = hit.collider.GetComponentInParent<Enemigo>();
            bool esJugador = hit.collider.GetComponentInParent<ControlJugador>();
            if (!esEnemigo && !esJugador) { Destroy(gameObject); return; }
        }
        transform.position += transform.forward * paso;
    }
}
