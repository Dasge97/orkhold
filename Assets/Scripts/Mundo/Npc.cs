using UnityEngine;

/// Un personaje del pueblo (07-lore.md §5) que dice una frase al usarlo con el botón de interactuar.
/// Los que enseñan el tutorial llevan además un Cartel; Ferra, la Herreria.
public class Npc : Interactuable
{
    public string nombre = "";
    [TextArea] public string[] frases = new string[0];

    int _siguiente;

    /// Última frase dicha (para los tests).
    public string UltimaFrase { get; private set; } = "";

    public override void Usar(ControlJugador jugador)
    {
        if (frases == null || frases.Length == 0) return;
        UltimaFrase = frases[_siguiente % frases.Length];
        _siguiente++;
        Avisos.Mostrar($"{nombre}: \"{UltimaFrase}\"", 3.5f);
    }
}
