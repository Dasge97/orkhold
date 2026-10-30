using System.Collections;
using UnityEngine;

/// Bombardero de la Forja Hundida (11-mazmorras.md §2): se queda lejos y lanza una bomba donde estás.
/// Sale un círculo de aviso y, al acabar, explota: hace daño a quien siga dentro.
public class Bombardero : Enemigo
{
    public float distanciaIdeal = 4.5f;
    public float cadenciaBomba = 3f;
    public float radioBomba = 1.2f;
    public float avisoBomba = 1.2f;

    /// Bombas lanzadas (para los tests).
    public int Bombas { get; private set; }

    protected override void Pensar(float d, Vector3 aJugador)
    {
        if (d > distanciaVer)
        {
            if (EstadoActual == Estado.Persigue || EstadoActual == Estado.Ataca) EstadoActual = Estado.Vuelve;
            base.Pensar(d, aJugador);
            return;
        }
        EstadoActual = Estado.Ataca;
        Mirar(aJugador);
        // Se acerca si está lejos y se aparta si está cerca.
        if (d > distanciaIdeal + 0.8f) { Mover(aJugador, velocidad); return; }
        if (d < distanciaIdeal - 1.5f) { Mover(-aJugador, velocidad); return; }
        Caer();
        if (Time.time >= _proximoAtaque) StartCoroutine(Lanzar());
        else Animar("idle");
    }

    IEnumerator Lanzar()
    {
        Bombas++;
        _proximoAtaque = Time.time + cadenciaBomba;
        _ocupadoHasta = Time.time + 0.5f;
        Animar("attack-melee-right", true);
        Vector3 donde = _jugador.position;
        AvisoArea.Crear(donde, radioBomba, avisoBomba);
        yield return new WaitForSeconds(avisoBomba);
        if (Vida.Muerto) yield break;
        Sonidos.Tocar("jefe", donde, 0.8f);
        if (_jugador && _vidaJugador && Plano(_jugador.position - donde).magnitude <= radioBomba)
            _vidaJugador.Danar(dano, donde);
    }
}
