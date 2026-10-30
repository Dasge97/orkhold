using System.Linq;
using UnityEngine;

/// Jugador automático para probar la red sin manos (tarea 7.8): con el argumento -bot, anda en
/// círculos por el pueblo y cada 2 s escribe en el registro dónde está y dónde ve a los demás.
/// Con -bot-golpear además ataca a lo que tenga cerca (para probar el botín contra la API).
[DefaultExecutionOrder(-30)]   // antes que JugadorRed, que manda los toques, y que ControlJugador, que los limpia
public class BotRed : MonoBehaviour
{
    float _t, _siguiente;
    bool _golpear;
    Vector3? _centro;
    Vector3 _ultimaPos;
    float _revisarAtasco, _apartarHasta;
    string _blanco = "";
    Transform _actual;
    float _mejorDist, _desde;
    readonly System.Collections.Generic.HashSet<Transform> _ignorados = new System.Collections.Generic.HashSet<Transform>();

    void Start() { _golpear = Configuracion.Bandera("-bot-golpear"); }

    void Update()
    {
        var yo = ControlJugador.Local;
        if (!yo) return;
        _t += Time.deltaTime;
        if (_golpear) { Cazar(yo); Informar(yo); return; }
        // Círculo de 2 m alrededor de donde apareció (lejos de las puertas), distinto según el nombre.
        _centro ??= yo.transform.position;
        float fase = (Partida.Actual.nombre ?? "").Length;
        Vector3 objetivo = _centro.Value + new Vector3(Mathf.Cos(_t * 1.1f + fase), 0f, Mathf.Sin(_t * 1.1f + fase)) * 2f;
        Vector3 d = objetivo - yo.transform.position; d.y = 0f;
        EntradaJuego.MoverPantalla = d.magnitude < 0.1f ? Vector2.zero : new Vector2(d.x, d.z).normalized;
        Informar(yo);
    }

    /// Va a por lo recogible más cercano o, si no hay, al orco vivo más cercano, y le pega.
    void Cazar(ControlJugador yo)
    {
        var p = yo.transform.position;
        var rec = FindObjectsByType<RecogibleRed>().OrderBy(r => (r.transform.position - p).sqrMagnitude).FirstOrDefault();
        if (rec && Vector3.Distance(rec.transform.position, p) < 6f)
        {
            _blanco = $"recogible {rec.name}@{rec.transform.position.x:F1},{rec.transform.position.y:F1},{rec.transform.position.z:F1}";
            if (Ir(yo, rec.transform.position, 0.8f) && Time.frameCount % 30 == 0) EntradaJuego.InteractuarPantalla = true;
            return;
        }
        // Lo más cercano entre árboles o rocas sin agotar y orcos vivos.
        var blancos = FindObjectsByType<FuenteRecurso>().Where(f => !f.Agotado).Select(f => f.transform)
            .Concat(FindObjectsByType<EnemigoRed>().Where(e => e.GetComponent<Enemigo>().Visible).Select(e => e.transform));
        var blanco = blancos.Where(t => !_ignorados.Contains(t)).OrderBy(t => (t.position - p).sqrMagnitude).FirstOrDefault();
        if (!blanco) { EntradaJuego.MoverPantalla = Vector2.zero; return; }
        // Si en 4 s no se acerca (está detrás de una valla o una casa), lo deja por imposible.
        float dist = Vector3.Distance(blanco.position, p);
        if (blanco != _actual) { _actual = blanco; _mejorDist = dist; _desde = Time.time; }
        else if (dist < _mejorDist - 0.3f) { _mejorDist = dist; _desde = Time.time; }
        else if (dist > 1.5f && Time.time - _desde > 4f) { _ignorados.Add(blanco); _actual = null; return; }
        _blanco = $"{blanco.name}@{blanco.position.x:F1},{blanco.position.z:F1}";
        if (Ir(yo, blanco.position, 1.2f) && Time.frameCount % 40 == 0) EntradaJuego.AtaquePantalla = true;
    }

    bool Ir(ControlJugador yo, Vector3 destino, float cerca)
    {
        Vector3 d = destino - yo.transform.position; d.y = 0f;
        bool llegado = d.magnitude <= cerca;
        Vector2 dir = llegado ? Vector2.zero : new Vector2(d.x, d.z).normalized;
        // Atascado contra algo: se aparta de lado un segundo.
        if (Time.time > _revisarAtasco)
        {
            var aqui = yo.transform.position;
            if (!llegado && (aqui - _ultimaPos).sqrMagnitude < 0.09f) _apartarHasta = Time.time + 1f;
            _ultimaPos = aqui; _revisarAtasco = Time.time + 1.5f;
        }
        if (Time.time < _apartarHasta) dir = new Vector2(-dir.y, dir.x);
        EntradaJuego.MoverPantalla = dir;
        return llegado;
    }

    void Informar(ControlJugador yo)
    {
        if (Time.time < _siguiente) return;
        _siguiente = Time.time + 2f;
        var otros = FindObjectsByType<JugadorRed>().Where(j => !j.IsOwner)
            .Select(j => $"{j.Nombre}@{j.transform.position.x:F1},{j.transform.position.z:F1}");
        Debug.Log($"BOT {Partida.Actual.nombre} en {yo.transform.position.x:F1},{yo.transform.position.z:F1} | VEO {string.Join(" ", otros)} | orcos {FindObjectsByType<EnemigoRed>().Length} | monedas {Partida.Inventario.monedas} madera {Partida.Inventario.madera} | blanco {_blanco} anim {yo.EstadoAnim}");
    }
}
