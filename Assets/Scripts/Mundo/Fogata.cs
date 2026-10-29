using UnityEngine;

/// Junto a la fogata, la vida se recupera del todo (01-diseno.md §4.5).
public class Fogata : MonoBehaviour
{
    public float radio = 1.7f;

    ControlJugador _jugador;

    void Update()
    {
        if (!_jugador) { _jugador = FindAnyObjectByType<ControlJugador>(); if (!_jugador) return; }
        var vida = _jugador.Vida;
        if (vida.Muerto || vida.actual >= vida.maxima) return;
        Vector3 d = _jugador.transform.position - transform.position; d.y = 0f;
        if (d.magnitude > radio) return;
        vida.CurarDelTodo();
        Avisos.Mostrar("Vida recuperada en la fogata", 1.5f);
    }
}
