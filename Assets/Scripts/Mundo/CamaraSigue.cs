using UnityEngine;

/// Cámara fija en ángulo que sigue al jugador con un poco de retraso.
public class CamaraSigue : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 desplazamiento = new Vector3(0f, 5.5f, -6.5f);
    public float suavidad = 8f;

    void LateUpdate()
    {
        if (!objetivo) return;
        Vector3 deseada = objetivo.position + desplazamiento;
        transform.position = Vector3.Lerp(transform.position, deseada, suavidad * Time.deltaTime);
        transform.LookAt(objetivo.position + Vector3.up * 0.5f);
    }
}
