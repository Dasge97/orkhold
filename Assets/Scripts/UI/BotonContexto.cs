using UnityEngine;
using UnityEngine.UI;

/// Cambia el icono del botón de interactuar según lo que haya cerca (recoger, puerta, cofre, tablón, construir,
/// hablar) y escribe debajo qué hará ("Hablar", "Recoger"...). Marca lo que vas a usar con un aro en el suelo
/// (15-interfaz.md §3 y §6). Sin nada cerca, muestra la mano medio transparente y sin texto.
public class BotonContexto : MonoBehaviour
{
    public Image icono;
    [Tooltip("Un sprite por valor de Interactuable.Icono, en el mismo orden.")]
    public Sprite[] sprites = new Sprite[0];

    ControlJugador _jugador;
    float _siguiente;
    Text _etiqueta;

    /// Icono que se muestra ahora (para los tests); null si no hay nada cerca.
    public Interactuable.Icono? Actual { get; private set; }
    /// Texto que se ve bajo el botón (para los tests); vacío si no hay nada cerca.
    public string Etiqueta => _etiqueta ? _etiqueta.text : "";

    void Start()
    {
        // Cartelito de madera debajo del botón.
        var marco = Ui.Marco(transform, "Etiqueta", "buttonLong_brown", new Vector2(0f, -110f), new Vector2(210f, 58f));
        marco.GetComponent<Image>().raycastTarget = false;
        _etiqueta = Ui.Texto(marco, "", 30, new Vector2(0f, 2f), new Vector2(200f, 50f));
        _etiqueta.font = Ui.FuenteTitulo;
        _etiqueta.color = Ui.Crema;
        marco.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Time.unscaledTime < _siguiente) return;
        _siguiente = Time.unscaledTime + 0.1f;
        _jugador = ControlJugador.Local;
        if (!_jugador) return;
        var cerca = Interactuable.MasCercano(_jugador.transform.position);
        Actual = cerca ? cerca.IconoBoton : (Interactuable.Icono?)null;
        int i = (int)(Actual ?? Interactuable.Icono.Mano);
        if (icono && i < sprites.Length && sprites[i]) icono.sprite = sprites[i];
        if (icono) icono.color = new Color(1f, 1f, 1f, cerca ? 1f : 0.45f);
        if (_etiqueta)
        {
            _etiqueta.text = cerca ? cerca.Verbo : "";
            _etiqueta.transform.parent.gameObject.SetActive(cerca);
        }
        AroInteraccion.Seguir(cerca);
    }
}
