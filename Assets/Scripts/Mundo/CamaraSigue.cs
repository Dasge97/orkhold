using UnityEngine;

/// Cámara fija en ángulo que sigue al jugador con un poco de retraso. Se sacude un poco con los golpes fuertes
/// y pone cámara lenta medio segundo al vencer a un jefe (15-interfaz.md §7).
public class CamaraSigue : MonoBehaviour
{
    static float _fuerza, _hasta, _duracion = 1f;
    static float _lentaHasta = -1f;

    /// Sacudida corta: "fuerza" en metros (0,1 = golpe normal).
    public static void Sacudir(float fuerza, float duracion = 0.25f)
    {
        if (Time.unscaledTime < _hasta && fuerza < _fuerza) return;
        _fuerza = fuerza; _duracion = duracion; _hasta = Time.unscaledTime + duracion;
    }

    /// Cámara lenta (el tiempo al 30 %) durante "segundos" reales.
    public static void Lenta(float segundos = 0.6f)
    {
        _lentaHasta = Time.unscaledTime + segundos;
        Time.timeScale = 0.3f;
    }

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
        if (_lentaHasta > 0f && Time.unscaledTime >= _lentaHasta) { _lentaHasta = -1f; Time.timeScale = 1f; }
        Vector3 deseada = objetivo.position + desplazamiento;
        transform.position = Vector3.Lerp(transform.position, deseada, suavidad * Time.deltaTime);
        transform.LookAt(objetivo.position + Vector3.up * 0.5f);
        float queda = _hasta - Time.unscaledTime;
        if (queda > 0f)
        {
            float f = _fuerza * (queda / _duracion);
            transform.position += transform.right * Random.Range(-f, f) + transform.up * Random.Range(-f, f);
        }
    }
}
