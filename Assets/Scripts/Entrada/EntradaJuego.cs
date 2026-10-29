using UnityEngine;

/// Punto único de entrada del jugador. Los controles en pantalla escriben aquí;
/// en el editor también funcionan el teclado (WASD, espacio, E, shift) para probar sin móvil.
public static class EntradaJuego
{
    public static Vector2 MoverPantalla;      // lo escribe el joystick
    public static bool AtaquePantalla;        // lo escriben los botones (un frame)
    public static bool InteractuarPantalla;
    public static bool CorrerPantalla;        // mantenido

    public static Vector2 Mover
    {
        get
        {
            if (MoverPantalla.sqrMagnitude > 0.01f) return MoverPantalla;
            var t = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(t, 1f);
        }
    }

    public static bool Ataque => AtaquePantalla || Input.GetKeyDown(KeyCode.Space);
    public static bool Interactuar => InteractuarPantalla || Input.GetKeyDown(KeyCode.E);
    public static bool Correr => CorrerPantalla || Input.GetKey(KeyCode.LeftShift);

    /// Se llama al final de cada frame para que los pulsos de botón duren un solo frame.
    public static void LimpiarPulsos()
    {
        AtaquePantalla = false;
        InteractuarPantalla = false;
    }
}
