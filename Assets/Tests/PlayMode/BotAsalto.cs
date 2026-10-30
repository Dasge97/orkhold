using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Jugador automático para probar el equilibrio del asalto (tarea 5.5, hasta que lo pruebe Daniel).
/// "Bueno": camino más barato al cofre por la rejilla (romper un muro cuesta según su vida, las
/// trampas se evitan) y pelea con los defensores que se acercan. "Malo": va en línea recta por el
/// camino más corto, pisa trampas y no se defiende.
public class BotAsalto : MonoBehaviour
{
    public bool bueno = true;

    Fuerte _f;
    Asalto _a;
    ControlJugador _j;
    List<Vector2Int> _camino = new List<Vector2Int>();
    bool _retirado, _contada;
    /// Veces que se ha retirado a recuperar vida.
    public int Retiradas { get; private set; }
    float _recalcular, _siguienteGolpe;

    void Start()
    {
        _f = Fuerte.Actual;
        _a = Asalto.Actual;
        _j = FindAnyObjectByType<ControlJugador>();
    }

    void Update()
    {
        if (!_a || _a.Estado != Asalto.Fin.EnCurso || !_j || _j.Vida.Muerto) { EntradaJuego.MoverPantalla = Vector2.zero; return; }

        // 1. Retirarse a la entrada a recuperar vida si queda poca y hay tiempo (solo el bueno).
        if (bueno)
        {
            var e0 = Enemigo.Todos.Where(x => x && x.enabled && !x.Vida.Muerto)
                .OrderBy(x => Plano(x.transform.position - _j.transform.position).magnitude).FirstOrDefault();
            if (!_retirado && _j.Vida.actual <= 3 && _a.Restante > 35f) { _retirado = true; Retiradas++; }
            if (_retirado && (_j.Vida.actual >= _j.Vida.maxima - 1 || _a.Restante < 30f)) _retirado = false;
            EntradaJuego.CorrerPantalla = _retirado;
            bool pegado = e0 && Plano(e0.transform.position - _j.transform.position).magnitude < 1.2f;
            if (_retirado && pegado)
            {
                // Le han alcanzado: se defiende.
                EntradaJuego.CorrerPantalla = false;
                EntradaJuego.MoverPantalla = Vector2.zero;
                Golpear();
                return;
            }
            if (_retirado)
            {
                // Huye corriendo hacia la entrada por donde se pueda andar, sin romper nada; se queda
                // una casilla antes de la llegada para no pisar la salida (salir es rendirse).
                var l = ReglasFuerte.Llegada(_f.Mapa) + Vector2Int.right;
                var c = Calcular(l, false);
                var aquiR = _f.Casilla(_j.transform.position);
                while (c.Count > 0 && c[0] == aquiR) c.RemoveAt(0);
                Mover(c.Count > 0 ? _f.Mundo(c[0].x, c[0].y) : _f.Mundo(l.x, l.y));
                return;
            }
        }

        // 2. Pelear con un defensor cercano (solo el bueno).
        if (bueno)
        {
            var e = Enemigo.Todos.Where(x => x && x.enabled && !x.Vida.Muerto)
                .OrderBy(x => Plano(x.transform.position - _j.transform.position).magnitude).FirstOrDefault();
            if (e && Plano(e.transform.position - _j.transform.position).magnitude < 1.5f)
            {
                EntradaJuego.MoverPantalla = Vector2.zero;
                Golpear();
                return;
            }
        }

        // 3. Camino hacia el cofre.
        if (Time.time >= _recalcular) { _camino = Calcular(ReglasFuerte.Cofre(_f.Mapa), true); _recalcular = Time.time + 0.4f; }
        var aqui = _f.Casilla(_j.transform.position);
        while (_camino.Count > 0 && _camino[0] == aqui) _camino.RemoveAt(0);
        if (_camino.Count == 0) { Mover(_a.PosicionCofre); return; }
        var siguiente = _camino[0];
        var muro = PiezaEn(siguiente);
        if (muro && muro.categoria == CatalogoPiezas.Categoria.Muro)
        {
            // Pegado al centro de su casilla, mirando al muro, a golpes.
            Vector3 centro = _f.Mundo(aqui.x, aqui.y);
            if (Plano(centro - _j.transform.position).magnitude > 0.15f) { Mover(centro); return; }
            EntradaJuego.MoverPantalla = Vector2.zero;
            Vector3 a = Plano(muro.transform.position - _j.transform.position);
            _j.transform.rotation = Quaternion.LookRotation(a);
            Golpear();
            return;
        }
        Mover(_f.Mundo(siguiente.x, siguiente.y));
    }

    void Golpear()
    {
        if (Time.time < _siguienteGolpe) return;
        _siguienteGolpe = Time.time + _j.duracionAtaque + 0.05f;
        EntradaJuego.AtaquePantalla = true;
    }

    /// La cámara mira al norte (+Z), así que el joystick va en coordenadas del mundo.
    void Mover(Vector3 destino)
    {
        Vector3 d = Plano(destino - _j.transform.position);
        EntradaJuego.MoverPantalla = d.magnitude < 0.05f ? Vector2.zero : new Vector2(d.x, d.z).normalized;
    }

    PiezaFuerte PiezaEn(Vector2Int c) => _f.Piezas.FirstOrDefault(p => p && p.x == c.x && p.z == c.y);

    /// Dijkstra en la rejilla de 4 vecinos hasta "cofre". Si "romper" es falso, los muros no se cruzan.
    List<Vector2Int> Calcular(Vector2Int cofre, bool romper)
    {
        var mapa = _f.Mapa;
        var origen = _f.Casilla(_j.transform.position);
        var piezas = new Dictionary<Vector2Int, PiezaFuerte>();
        foreach (var p in _f.Piezas) if (p) piezas[new Vector2Int(p.x, p.z)] = p;

        float Coste(Vector2Int c)
        {
            char l = ReglasFuerte.Letra(mapa, c.x, c.y);
            if (l == 'v' || l == ' ') return -1f;
            if (!piezas.TryGetValue(c, out var p)) return 1f;
            if (p.categoria == CatalogoPiezas.Categoria.Muro) return romper ? 1f + p.vida.actual * 1.6f : -1f;
            if (p.categoria == CatalogoPiezas.Categoria.Trampa) return bueno ? 6f : 1f;
            return bueno ? 3f : 1f;
        }

        var dist = new Dictionary<Vector2Int, float> { [origen] = 0f };
        var previo = new Dictionary<Vector2Int, Vector2Int>();
        var abiertos = new List<Vector2Int> { origen };
        while (abiertos.Count > 0)
        {
            var u = abiertos.OrderBy(c => dist[c]).First();
            abiertos.Remove(u);
            if (u == cofre) break;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var v = u + d;
                if (v.x < 0 || v.y < 0 || v.x >= mapa.Ancho || v.y >= mapa.Alto) continue;
                float c = v == cofre ? 1f : Coste(v);
                if (c < 0f) continue;
                float nd = dist[u] + c;
                if (dist.TryGetValue(v, out var vieja) && vieja <= nd) continue;
                dist[v] = nd; previo[v] = u;
                if (!abiertos.Contains(v)) abiertos.Add(v);
            }
        }
        var camino = new List<Vector2Int>();
        if (!previo.ContainsKey(cofre)) return camino;
        for (var c = cofre; c != origen; c = previo[c]) camino.Insert(0, c);
        return camino;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }
}
