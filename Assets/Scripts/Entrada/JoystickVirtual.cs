using UnityEngine;
using UnityEngine.EventSystems;

/// Joystick táctil: una base fija y una bolita que sigue el dedo dentro de un radio.
public class JoystickVirtual : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform bolita;
    public float radio = 110f;

    RectTransform _base;

    void Awake() { _base = GetComponent<RectTransform>(); }

    public void OnPointerDown(PointerEventData e) { OnDrag(e); }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_base, e.position, e.pressEventCamera, out var local);
        var v = Vector2.ClampMagnitude(local, radio);
        bolita.anchoredPosition = v;
        EntradaJuego.MoverPantalla = v / radio;
    }

    public void OnPointerUp(PointerEventData e)
    {
        bolita.anchoredPosition = Vector2.zero;
        EntradaJuego.MoverPantalla = Vector2.zero;
    }
}
