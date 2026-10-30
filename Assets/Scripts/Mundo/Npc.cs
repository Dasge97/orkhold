using UnityEngine;

/// Un personaje del pueblo (07-lore.md §5) que dice una frase al usarlo con el botón de interactuar.
/// Habla en la caja de diálogo (15-interfaz.md §5) y lleva su nombre y oficio encima.
public class Npc : Interactuable
{
    public string nombre = "";
    [TextArea] public string[] frases = new string[0];

    int _siguiente;

    public override Icono IconoBoton => Icono.Hablar;
    public override string Verbo => "Hablar";

    void Start() { Rotulo.Poner(gameObject, Rotulo.Tipo.Personaje, nombre, Cartel.Oficio(nombre)); }

    /// Última frase dicha (para los tests).
    public string UltimaFrase { get; private set; } = "";

    public override void Usar(ControlJugador jugador)
    {
        if (frases == null || frases.Length == 0) return;
        UltimaFrase = frases[_siguiente % frases.Length];
        _siguiente++;
        Vector3 d = jugador ? jugador.transform.position - transform.position : Vector3.zero; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        Dialogo.Abrir(transform, nombre, Cartel.Oficio(nombre), nombre, UltimaFrase);
    }
}
