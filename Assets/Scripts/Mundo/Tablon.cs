/// El tablón de la plaza: abre el ranking y el historial de asaltos (01-diseno.md §6.5).
public class Tablon : Interactuable
{
    public override Icono IconoBoton => Icono.Tablon;

    public override void Usar(ControlJugador jugador) => PantallaRanking.Abrir();
}
