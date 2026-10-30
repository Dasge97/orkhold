/// Ferra, la herrera (07-lore.md §5.4): con el botón de interactuar abre la fabricación de herramientas de saqueo.
/// Si lleva Cartel (Ferra en el pueblo), se usa desde su diálogo ("Fabricar") y no compite con él por el botón.
public class Herreria : Interactuable
{
    public override bool Disponible => base.Disponible && !GetComponent<Cartel>();

    public override void Usar(ControlJugador jugador)
    {
        if (!Sesion.Conectado) { Avisos.Mostrar("Ferra: \"Sin servidor no hay forja.\"", 2.5f); return; }
        PantallaFerra.Abrir();
    }
}
