using UnityEngine;

/// Fuera de combate, el jugador recupera 1 de vida cada 10 s (01-diseno.md §4.5).
/// "Fuera de combate": sin dar ni recibir golpes durante los últimos 5 s.
[RequireComponent(typeof(Vida), typeof(ControlJugador))]
public class Regeneracion : MonoBehaviour
{
    public float cadaSegundos = 10f;
    public float sinCombateSegundos = 5f;

    Vida _vida;
    ControlJugador _jugador;
    float _acumulado;

    void Awake()
    {
        _vida = GetComponent<Vida>();
        _jugador = GetComponent<ControlJugador>();
    }

    void Update()
    {
        bool enCombate = Time.time - _jugador.UltimoCombate < sinCombateSegundos;
        if (_vida.Muerto || enCombate || _vida.actual >= _vida.maxima) { _acumulado = 0f; return; }
        _acumulado += Time.deltaTime;
        if (_acumulado < cadaSegundos) return;
        _acumulado = 0f;
        _vida.Curar(1);
    }
}
