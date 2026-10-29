using System.Collections.Generic;
using UnityEngine;

/// Algo que el jugador usa con el botón de interactuar: recogibles, puertas, cofres, tablón...
/// Se apuntan solos a una lista para buscar el más cercano sin recorrer la escena.
public abstract class Interactuable : MonoBehaviour
{
    public enum Icono { Mano, Recoger, Puerta, Cofre, Tablon, Construir }

    public static readonly List<Interactuable> Todos = new List<Interactuable>();

    [Tooltip("Distancia máxima desde el jugador para poder usarlo.")]
    public float alcance = 1.3f;

    public virtual Icono IconoBoton => Icono.Mano;
    public virtual bool Disponible => isActiveAndEnabled;

    /// Lo que pasa al usarlo.
    public abstract void Usar(ControlJugador jugador);

    protected virtual void OnEnable() { Todos.Add(this); }
    protected virtual void OnDisable() { Todos.Remove(this); }

    /// El más cercano a "posicion" que esté dentro de su alcance, o null.
    public static Interactuable MasCercano(Vector3 posicion)
    {
        Interactuable mejor = null;
        float dMejor = float.MaxValue;
        foreach (var i in Todos)
        {
            if (!i || !i.Disponible) continue;
            Vector3 d = i.transform.position - posicion; d.y = 0f;
            float dist = d.magnitude;
            if (dist <= i.alcance && dist < dMejor) { dMejor = dist; mejor = i; }
        }
        return mejor;
    }
}
