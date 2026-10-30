using UnityEngine;

/// Nombre de otro jugador sobre su cabeza (tarea 7.3): pone su Rotulo y lo mantiene al día con el nombre que llega por red.
public class NombreEncima : MonoBehaviour
{
    public JugadorRed jugador;
    Rotulo _rotulo;

    void Start() { _rotulo = Rotulo.Poner(gameObject, Rotulo.Tipo.Jugador, ""); }

    void Update()
    {
        if (_rotulo && jugador) _rotulo.texto = jugador.Nombre;
    }
}
