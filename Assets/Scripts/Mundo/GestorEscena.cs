using System;
using UnityEngine;

/// Uno por escena. Al cargar, pone al jugador junto a la puerta de llegada (Viaje.llegarA)
/// o en la aparición normal del mapa. También decide qué pasa cuando el jugador muere.
[DefaultExecutionOrder(-50)]
public class GestorEscena : MonoBehaviour
{
    public enum Muerte
    {
        Reaparecer,     // pueblo y fuerte: pantalla oscura 1 s y vuelta a la aparición
        SalirConMitad,  // mazmorra: se pierde la mitad de lo recogido y se vuelve al pueblo
        Avisar,         // asalto: lo resuelve quien escuche AlMorirJugador
    }
    public enum Suelo { Hierba, Piedra }

    [Tooltip("Aparición normal (fogata en el pueblo, entrada en la mazmorra y el fuerte).")]
    public Vector3 aparicion;
    [Tooltip("Hacia dónde mira el jugador al aparecer (grados).")]
    public float giroAparicion;
    public Muerte alMorir = Muerte.Reaparecer;
    public Suelo suelo = Suelo.Hierba;
    [Tooltip("Puerta del pueblo junto a la que se aparece al salir de esta escena por muerte.")]
    public string puertaVuelta = "";
    [Tooltip("Es una mazmorra: lo recogido desde que se entra es el botín de la entrada.")]
    public bool esMazmorra;
    [Tooltip("Número de la mazmorra (1 catacumbas, 2 Forja Hundida, 3 Torre del Vigía, 4 Pozo de los Clanes).")]
    public int numeroMazmorra = 1;
    [Tooltip("Es el pueblo: lo recogido se manda a la API como botín del pueblo.")]
    public bool esPueblo;

    public static GestorEscena Actual { get; private set; }
    /// Para el asalto (fase 5): se dispara cuando el jugador muere y alMorir es Avisar.
    public static event Action AlMorirJugador;

    void Awake()
    {
        Actual = this;
        if (esMazmorra) Partida.EmpezarEntrada();
        // En el pueblo online el jugador lo crea la red (RedPueblo lo coloca al aparecer).
        if (FindAnyObjectByType<RedPueblo>()) return;
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

    /// Lo llama el jugador cuando termina la animación de morir.
    public void JugadorMuerto(ControlJugador jugador)
    {
        switch (alMorir)
        {
            case Muerte.Reaparecer:
                if (jugador == ControlJugador.Local)
                {
                    Avisos.Mostrar("Has muerto", 1.5f);
                    Transicion.Fundido(() => { if (jugador.TieneAutoridad) jugador.Revivir(aparicion, giroAparicion); }, 1f);
                }
                else if (jugador.TieneAutoridad) StartCoroutine(RevivirLuego(jugador, 1.5f));   // servidor: jugador de otro móvil
                break;
            case Muerte.SalirConMitad:
                int perdido = Partida.PerderMitadDeLaEntrada();
                Avisos.Mostrar(perdido > 0 ? $"Has muerto: pierdes la mitad del botín ({perdido})" : "Has muerto", 2.5f);
                Viaje.llegarA = puertaVuelta;
                Transicion.Ir("Pueblo", 1f);
                break;
            case Muerte.Avisar:
                if (AlMorirJugador != null) AlMorirJugador();
                else { Viaje.llegarA = puertaVuelta; Transicion.Ir("Pueblo", 1f); }
                break;
        }
    }

    System.Collections.IEnumerator RevivirLuego(ControlJugador j, float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (j) j.Revivir(aparicion, giroAparicion);
    }

    /// Mueve al jugador sin que el CharacterController lo devuelva a su sitio.
    public static void Colocar(Transform jugador, Vector3 donde, float giro) => Colocar(jugador, donde, giro, true);

    public static void Colocar(Transform jugador, Vector3 donde, float giro, bool moverCamara)
    {
        var cc = jugador.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        jugador.SetPositionAndRotation(donde + Vector3.up * 0.05f, Quaternion.Euler(0f, giro, 0f));
        if (cc) cc.enabled = true;
        if (!moverCamara) return;
        var cam = FindAnyObjectByType<CamaraSigue>();
        if (cam) cam.Saltar();
    }

    /// Dónde aparece quien llega por la puerta "llegarA" (vacío = aparición normal). Lo usa el servidor del pueblo.
    public void Llegada(string llegarA, out Vector3 donde, out float giro)
    {
        donde = aparicion; giro = giroAparicion;
        if (string.IsNullOrEmpty(llegarA)) return;
        foreach (var p in FindObjectsByType<Puerta>())
            if (p.id == llegarA)
            {
                donde = p.llegada;
                Vector3 fuera = p.llegada - p.transform.position; fuera.y = 0f;
                if (fuera.sqrMagnitude > 0.01f) giro = Quaternion.LookRotation(fuera).eulerAngles.y;
                return;
            }
    }
}
