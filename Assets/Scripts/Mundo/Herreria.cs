/// Ferra, la herrera (07-lore.md §5.4): con el botón de interactuar abre la fabricación de herramientas de saqueo.
public class Herreria : Interactuable
{
    public override void Usar(ControlJugador jugador)
    {
        if (!Sesion.Conectado) { Avisos.Mostrar("Ferra: \"Sin servidor no hay forja.\"", 2.5f); return; }
        PantallaFerra.Abrir();
    }
}
