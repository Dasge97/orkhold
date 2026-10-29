using System.Collections.Generic;
using UnityEngine;

/// Sonidos del juego (Kenney RPG Audio e Impact Sounds, CC0). Cada nombre es un grupo de clips
/// parecidos; se elige uno al azar. El volumen baja con la distancia al jugador.
public class Sonidos : MonoBehaviour
{
    /// grupo → prefijos de los ficheros en Resources/Sonidos
    static readonly Dictionary<string, string[]> Grupos = new Dictionary<string, string[]>
    {
        { "pasos_hierba", new[] { "footstep_grass_" } },
        { "pasos_piedra", new[] { "footstep_concrete_" } },
        { "golpe", new[] { "impactPunch_medium_" } },
        { "golpe_madera", new[] { "impactWood_medium_" } },
        { "golpe_roca", new[] { "impactMining_" } },
        { "talar", new[] { "chop" } },
        { "recoger_moneda", new[] { "handleCoins" } },
        { "recoger", new[] { "dropLeather" } },
        { "dano", new[] { "impactSoft_medium_" } },
        { "morir", new[] { "impactPunch_heavy_" } },
        { "romper_muro", new[] { "impactPlank_medium_" } },
        { "puerta", new[] { "doorOpen_" } },
        { "jefe", new[] { "impactBell_heavy_" } },
    };

    public static bool Activo = true;
    public static float Volumen = 0.8f;

    /// Último grupo tocado (para los tests).
    public static string Ultimo { get; private set; } = "";

    static Sonidos _instancia;
    readonly Dictionary<string, List<AudioClip>> _clips = new Dictionary<string, List<AudioClip>>();
    readonly List<AudioSource> _fuentes = new List<AudioSource>();
    int _siguiente;

    static Sonidos Instancia
    {
        get
        {
            if (_instancia) return _instancia;
            var go = new GameObject("Sonidos");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<Sonidos>();
            _instancia.Cargar();
            return _instancia;
        }
    }

    void Cargar()
    {
        var todos = Resources.LoadAll<AudioClip>("Sonidos");
        foreach (var g in Grupos)
        {
            var lista = new List<AudioClip>();
            foreach (var c in todos)
                foreach (var pre in g.Value)
                    if (c.name.StartsWith(pre)) lista.Add(c);
            _clips[g.Key] = lista;
        }
        for (int i = 0; i < 10; i++)
        {
            var f = gameObject.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            _fuentes.Add(f);
        }
    }

    /// Cuántos clips tiene un grupo (para comprobar que están todos).
    public static int Clips(string grupo) => Instancia._clips.TryGetValue(grupo, out var l) ? l.Count : 0;
    public static IEnumerable<string> NombresGrupos => Grupos.Keys;

    /// Toca un sonido del grupo. Si se da "donde", se atenúa con la distancia al jugador (hasta 14 m).
    public static void Tocar(string grupo, Vector3? donde = null, float volumen = 1f)
    {
        Ultimo = grupo;
        if (!Activo) return;
        var s = Instancia;
        if (!s._clips.TryGetValue(grupo, out var lista) || lista.Count == 0) return;
        float v = volumen * Volumen;
        if (donde.HasValue)
        {
            var oyente = Camera.main ? Camera.main.transform.position : Vector3.zero;
            var jugador = FindAnyObjectByType<ControlJugador>();
            if (jugador) oyente = jugador.transform.position;
            float d = Vector3.Distance(oyente, donde.Value);
            v *= Mathf.Clamp01(1f - d / 14f);
            if (v <= 0.01f) return;
        }
        var f = s._fuentes[s._siguiente];
        s._siguiente = (s._siguiente + 1) % s._fuentes.Count;
        f.pitch = Random.Range(0.92f, 1.08f);
        f.PlayOneShot(lista[Random.Range(0, lista.Count)], v);
    }
}
