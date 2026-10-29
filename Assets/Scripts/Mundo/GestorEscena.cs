using UnityEngine;

/// Uno por escena. Al cargar, pone al jugador junto a la puerta de llegada (Viaje.llegarA)
/// o en la aparición normal del mapa.
[DefaultExecutionOrder(-50)]
public class GestorEscena : MonoBehaviour
{
    [Tooltip("Aparición normal (fogata en el pueblo, entrada en la mazmorra y el fuerte).")]
    public Vector3 aparicion;
    [Tooltip("Hacia dónde mira el jugador al aparecer (grados).")]
    public float giroAparicion;

    public static GestorEscena Actual { get; private set; }

    void Awake()
    {
        Actual = this;
        var jugador = FindAnyObjectByType<ControlJugador>();
        if (!jugador) return;
        Vector3 donde = aparicion;
        float giro = giroAparicion;
        if (!string.IsNullOrEmpty(Viaje.llegarA))
        {
            foreach (var p in FindObjectsByType<Puerta>())
                if (p.id == Viaje.llegarA)
                {
                    donde = p.llegada;
                    Vector3 fuera = p.llegada - p.transform.position; fuera.y = 0f;
                    if (fuera.sqrMagnitude > 0.01f) giro = Quaternion.LookRotation(fuera).eulerAngles.y;
                    break;
                }
            Viaje.llegarA = "";
        }
        Colocar(jugador.transform, donde, giro);
    }

    /// Mueve al jugador sin que el CharacterController lo devuelva a su sitio.
    public static void Colocar(Transform jugador, Vector3 donde, float giro)
    {
        var cc = jugador.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        jugador.SetPositionAndRotation(donde + Vector3.up * 0.05f, Quaternion.Euler(0f, giro, 0f));
        if (cc) cc.enabled = true;
        var cam = FindAnyObjectByType<CamaraSigue>();
        if (cam) cam.Saltar();
    }
}
