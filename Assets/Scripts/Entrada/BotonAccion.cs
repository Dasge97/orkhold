using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Botón flotante de acción. "Pulso" dispara una vez al tocar; "Mantenido" está activo mientras se toca.
public class BotonAccion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum Tipo { Ataque, Interactuar, Correr }
    public Tipo tipo;

    Image _img;

    void Awake() { _img = GetComponent<Image>(); }

    public void OnPointerDown(PointerEventData e)
    {
        if (_img) _img.color = new Color(1f, 1f, 1f, 0.6f);
        switch (tipo)
        {
            case Tipo.Ataque: EntradaJuego.AtaquePantalla = true; break;
            case Tipo.Interactuar: EntradaJuego.InteractuarPantalla = true; break;
            case Tipo.Correr: EntradaJuego.CorrerPantalla = true; break;
        }
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (_img) _img.color = Color.white;
        if (tipo == Tipo.Correr) EntradaJuego.CorrerPantalla = false;
    }
}
