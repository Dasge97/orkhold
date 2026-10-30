using System.Collections.Generic;
using UnityEngine;

/// Nombre sobre la cabeza de un personaje, enemigo o puerta (15-interfaz.md §4). Solo guarda qué decir;
/// lo dibuja Rotulos, que pone como mucho 12 a la vez, los más cercanos.
public class Rotulo : MonoBehaviour
{
    public enum Tipo { Jugador, Personaje, Enemigo, Puerta }

    public static readonly List<Rotulo> Todos = new List<Rotulo>();

    public static readonly Color ColorJugador = new Color(1f, 0.97f, 0.9f);
    public static readonly Color ColorPersonaje = new Color(1f, 0.8f, 0.3f);
    public static readonly Color ColorEnemigo = new Color(1f, 0.4f, 0.32f);
    public static readonly Color ColorPuerta = new Color(0.9f, 0.85f, 0.7f);

    public Tipo tipo = Tipo.Jugador;
    public string texto = "";
    public string subtitulo = "";
    public Color color = ColorJugador;
    [Tooltip("Altura sobre el suelo (se calcula con los modelos si es 0).")]
    public float altura;
    [Tooltip("Más lejos de esto, no se ve.")]
    public float distancia = 16f;
    [Tooltip("Si tiene vida, una barra pequeña debajo del nombre.")]
    public Vida vida;
    [Tooltip("Exclamación amarilla encima: tiene algo nuevo que contar.")]
    public bool novedad;

    /// Pone (o devuelve) el rótulo de "g".
    public static Rotulo Poner(GameObject g, Tipo tipo, string texto, string subtitulo = "")
    {
        var r = g.GetComponent<Rotulo>();
        if (!r) r = g.AddComponent<Rotulo>();
        r.tipo = tipo; r.texto = texto; r.subtitulo = subtitulo;
        r.color = tipo == Tipo.Personaje ? ColorPersonaje : tipo == Tipo.Enemigo ? ColorEnemigo : tipo == Tipo.Puerta ? ColorPuerta : ColorJugador;
        r.distancia = tipo == Tipo.Puerta ? 5f : tipo == Tipo.Enemigo ? 8f : 16f;
        if (tipo == Tipo.Enemigo) r.vida = g.GetComponent<Vida>();
        Rotulos.Asegurar();
        return r;
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        if (altura > 0f) return;
        // Con cápsula (jugadores, enemigos), su altura: el arma o el modelo cambiado no la estiran.
        var cc = GetComponent<CharacterController>();
        if (cc) { altura = (cc.center.y + cc.height / 2f) * transform.lossyScale.y + 0.3f; return; }
        // Encima del modelo más alto (sin contar partículas). En los personajes animados, la malla en reposo:
        // los bounds de un SkinnedMeshRenderer son mucho mayores que el personaje.
        float max = 0f;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            float alto = r is SkinnedMeshRenderer s && s.sharedMesh
                ? s.transform.TransformPoint(s.sharedMesh.bounds.max).y
                : r.bounds.max.y;
            max = Mathf.Max(max, alto - transform.position.y);
        }
        altura = max > 0.2f ? max + 0.25f : 1.4f;
    }

    /// Los enemigos solo enseñan nombre y barra si les acaban de pegar o estás cerca (lo decide la distancia).
    public bool Golpeado => vida && Time.time - vida.UltimoDano < 4f;

    public Vector3 Punto => transform.position + Vector3.up * altura;
}
