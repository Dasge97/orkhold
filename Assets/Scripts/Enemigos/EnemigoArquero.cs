using UnityEngine;

/// Orco arquero (01-diseno.md §4.2): se queda a 4 m, lanza una lanza cada 2 s y huye si te acercas a 2 m.
public class EnemigoArquero : Enemigo
{
    public float distanciaIdeal = 4f;
    public float distanciaHuir = 2f;
    public float cadenciaDisparo = 2f;
    public GameObject proyectil;

    /// Disparos hechos (para los tests).
    public int Disparos { get; private set; }


    protected override void Pensar(float d, Vector3 aJugador)
    {
        if (d > distanciaVer)
        {
            // Sin jugador a la vista: comportamiento normal (quieto, patrulla o volver).
            if (EstadoActual == Estado.Persigue || EstadoActual == Estado.Ataca) EstadoActual = Estado.Vuelve;
            base.Pensar(d, aJugador);
            return;
        }
        EstadoActual = Estado.Ataca;
        Mirar(aJugador);
        if (d < distanciaHuir)
        {
            // Huye mirando al jugador: hacia atrás, o en diagonal o de lado si detrás hay muro.
            Vector3 huida = DireccionLibre(-aJugador.normalized);
            if (_cc.enabled) _cc.Move((huida * velocidad + Vector3.down * 2f) * Time.deltaTime);
            Animar("walk");
            return;
        }
        if (d > distanciaIdeal + 0.5f) { Mover(aJugador, velocidad); return; }
        Caer();
        if (Time.time >= _proximoAtaque) Disparar(aJugador);
        else Animar("holding-right");
    }

    /// Primera dirección sin obstáculos a 1 m: la pedida, girada 45° a cada lado, 90° a cada lado.
    Vector3 DireccionLibre(Vector3 deseada)
    {
        foreach (float angulo in new[] { 0f, 45f, -45f, 90f, -90f, 135f, -135f })
        {
            Vector3 d = Quaternion.Euler(0f, angulo, 0f) * deseada;
            Vector3 origen = transform.position + Vector3.up * 0.4f;
            bool choca = false;
            foreach (var h in Physics.SphereCastAll(origen, 0.2f, d, 1f, ~0, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(transform) && !h.collider.GetComponentInParent<ControlJugador>()) { choca = true; break; }
            if (!choca) return d;
        }
        return deseada;
    }

    void Disparar(Vector3 aJugador)
    {
        _proximoAtaque = Time.time + cadenciaDisparo;
        _ocupadoHasta = Time.time + 0.35f;
        Animar("holding-right-shoot", true);
        if (!proyectil) return;
        Vector3 origen = transform.position + Vector3.up * 0.45f + transform.forward * 0.35f;
        var p = Instantiate(proyectil, origen, Quaternion.LookRotation(aJugador.normalized));
        p.SetActive(true);
        var pr = p.GetComponent<Proyectil>();
        if (pr) { pr.dano = dano; pr.dueno = transform; }
        Disparos++;
    }
}
