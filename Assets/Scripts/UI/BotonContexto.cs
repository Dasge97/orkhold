using UnityEngine;
using UnityEngine.UI;

/// Cambia el icono del botón de interactuar según lo que haya cerca (recoger, puerta, cofre,
/// tablón, construir). Sin nada cerca, muestra la mano medio transparente.
public class BotonContexto : MonoBehaviour
{
    public Image icono;
    [Tooltip("Un sprite por valor de Interactuable.Icono, en el mismo orden.")]
    public Sprite[] sprites = new Sprite[0];

    ControlJugador _jugador;
    float _siguiente;

    /// Icono que se muestra ahora (para los tests); null si no hay nada cerca.
    public Interactuable.Icono? Actual { get; private set; }

    void Update()
    {
        if (Time.unscaledTime < _siguiente) return;
        _siguiente = Time.unscaledTime + 0.1f;
        if (!_jugador) { _jugador = FindAnyObjectByType<ControlJugador>(); if (!_jugador) return; }
        var cerca = Interactuable.MasCercano(_jugador.transform.position);
        Actual = cerca ? cerca.IconoBoton : (Interactuable.Icono?)null;
        int i = (int)(Actual ?? Interactuable.Icono.Mano);
        if (icono && i < sprites.Length && sprites[i]) icono.sprite = sprites[i];
        if (icono) icono.color = new Color(1f, 1f, 1f, cerca ? 1f : 0.45f);
    }
}
