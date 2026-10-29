using UnityEngine;

/// Objeto del suelo que el jugador recoge con el botón de interactuar.
public class Recogible : MonoBehaviour
{
    public enum Clase { Moneda, Pocion }
    public Clase clase;
    public float giro = 90f;

    float _y0;

    void Start() { _y0 = transform.position.y; }

    void Update()
    {
        transform.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        var p = transform.position;
        p.y = _y0 + Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.position = p;
    }
}
