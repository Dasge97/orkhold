using UnityEngine;

/// Un cofre, un barril o el cofre central durante el saqueo (08-formato-y-pvp.md §4.2): el botón de
/// interactuar lo "mira" (1 s parado) y el servidor responde "Pesa." o "Suena hueco.".
public class Mirable : Interactuable
{
    public int x, z;

    public override Icono IconoBoton => Icono.Cofre;
    public override string Verbo => "Mirar";
    public override bool Disponible => base.Disponible && Saqueo.Actual && Saqueo.Actual.PuedeMirar;

    public override void Usar(ControlJugador jugador) { if (Saqueo.Actual) Saqueo.Actual.Mirar(x, z); }
}
