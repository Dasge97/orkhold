using UnityEngine;

/// Cámara fija en ángulo que sigue al jugador con un poco de retraso.
public class CamaraSigue : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 desplazamiento = new Vector3(0f, 5.5f, -6.5f);
    public float suavidad = 8f;

    void Start()
    {
        if (!objetivo && ControlJugador.Local) objetivo = ControlJugador.Local.transform;
        Saltar();
    }

    /// Coloca la cámara de golpe en su sitio (al aparecer o al cambiar de escena).
    public void Saltar()
    {
        if (!objetivo) return;
        transform.position = objetivo.position + desplazamiento;
        transform.LookAt(objetivo.position + Vector3.up * 0.5f);
    }

    void LateUpdate()
    {
        if (!objetivo)
        {
            // En el pueblo online el jugador aparece después.
            if (!ControlJugador.Local) return;
            objetivo = ControlJugador.Local.transform;
            ControlJugador.Local.cameraRef = transform;
            Saltar();
        }
        Vector3 deseada = objetivo.position + desplazamiento;
        transform.position = Vector3.Lerp(transform.position, deseada, suavidad * Time.deltaTime);
        transform.LookAt(objetivo.position + Vector3.up * 0.5f);
    }
}
