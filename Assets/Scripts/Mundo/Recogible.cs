using UnityEngine;

/// Objeto del suelo que el jugador recoge con el botón de interactuar.
public class Recogible : Interactuable
{
    public Recurso recurso;
    public int cantidad = 1;
    public float giro = 90f;

    float _y0;

    public override Icono IconoBoton => Icono.Recoger;

    void Start() { _y0 = transform.position.y; }

    void Update()
    {
        transform.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        var p = transform.position;
        p.y = _y0 + Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.position = p;
    }

    public override void Usar(ControlJugador jugador)
    {
        jugador.Recoger(this);
        Destroy(gameObject);
    }
}
