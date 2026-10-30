using UnityEngine;

/// Junto a la fogata, la vida se recupera del todo (01-diseno.md §4.5).
public class Fogata : MonoBehaviour
{
    public float radio = 1.7f;

    void Update()
    {
        // Cura quien tiene autoridad (el servidor en el pueblo online); el aviso, en el móvil del jugador.
        foreach (var j in ControlJugador.Todos)
        {
            if (!j || !j.TieneAutoridad) continue;
            var vida = j.Vida;
            if (vida.Muerto || vida.actual >= vida.maxima) continue;
            Vector3 d = j.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > radio) continue;
            vida.CurarDelTodo();
            if (j == ControlJugador.Local) Avisos.Mostrar("Vida recuperada en la fogata", 1.5f);
        }
    }
}
