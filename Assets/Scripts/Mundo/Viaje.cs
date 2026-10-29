/// Lo que se recuerda al cambiar de escena: a qué puerta hay que aparecer al llegar.
public static class Viaje
{
    /// Id de la puerta de la escena de destino junto a la que aparece el jugador. Vacío = aparición normal.
    public static string llegarA = "";
    /// Escena de la que se viene (para volver en caso de muerte o abandono).
    public static string escenaAnterior = "";
}
