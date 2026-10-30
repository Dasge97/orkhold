using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Dibuja los rótulos (nombres sobre las cabezas, 15-interfaz.md §4) en un lienzo de pantalla: nítidos a cualquier
/// distancia, con contorno, y como mucho 12 a la vez (los más cercanos). Se crea solo y sobrevive a los cambios de escena.
public class Rotulos : MonoBehaviour
{
    const int Maximo = 12;

    static Rotulos _instancia;

    RectTransform[] _raiz;
    Text[] _nombre, _sub;
    Image[] _barra, _novedad;
    readonly List<Rotulo> _visibles = new List<Rotulo>();

    /// Rótulos dibujados ahora (para los tests).
    public static IReadOnlyList<Rotulo> Visibles => _instancia ? _instancia._visibles : (IReadOnlyList<Rotulo>)new Rotulo[0];

    public static void Asegurar()
    {
        if (_instancia) return;
        var go = Ui.Lienzo("Rotulos", -10);
        Destroy(go.GetComponent<GraphicRaycaster>());
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<Rotulos>();
        _instancia.Montar();
    }

    void Montar()
    {
        _raiz = new RectTransform[Maximo];
        _nombre = new Text[Maximo]; _sub = new Text[Maximo];
        _barra = new Image[Maximo]; _novedad = new Image[Maximo];
        for (int i = 0; i < Maximo; i++)
        {
            var g = new GameObject("Rotulo " + i, typeof(RectTransform));
            g.transform.SetParent(transform, false);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.sizeDelta = new Vector2(420f, 120f);
            rt.pivot = new Vector2(0.5f, 0f);
            _raiz[i] = rt;
            _nombre[i] = Ui.Texto(rt, "", 30, new Vector2(0f, 26f), new Vector2(420f, 40f));
            _nombre[i].font = Ui.FuenteTitulo;
            _nombre[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Contorno(_nombre[i], 2f);
            _sub[i] = Ui.Texto(rt, "", 22, new Vector2(0f, 0f), new Vector2(420f, 28f));
            _sub[i].color = new Color(0.92f, 0.86f, 0.72f);
            Ui.Contorno(_sub[i], 1.5f);
            _barra[i] = Ui.Barra(rt, "barRed", new Vector2(0f, 4f), new Vector2(120f, 16f));
            _novedad[i] = Ui.Icono(rt, "icono-exclamation", new Vector2(0f, 78f), new Vector2(46f, 46f));
            _novedad[i].color = new Color(1f, 0.85f, 0.2f);
            Ui.Contorno(_novedad[i], 2f);
            g.SetActive(false);
        }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        _visibles.Clear();
        if (cam)
        {
            Vector3 desde = ControlJugador.Local ? ControlJugador.Local.transform.position : cam.transform.position;
            foreach (var r in Rotulo.Todos)
            {
                if (!r || string.IsNullOrEmpty(r.texto) || (r.vida && r.vida.Muerto)) continue;
                float d = Vector3.Distance(desde, r.transform.position);
                if (d > r.distancia && !(r.tipo == Rotulo.Tipo.Enemigo && r.Golpeado && d < 16f)) continue;
                if (r.tipo == Rotulo.Tipo.Puerta && d < 1.6f) continue;   // encima de la puerta ya sabes adónde vas
                var p = cam.WorldToViewportPoint(r.Punto);
                if (p.z <= 0f || p.x < -0.1f || p.x > 1.1f || p.y < -0.1f || p.y > 1.1f) continue;
                _visibles.Add(r);
            }
            if (_visibles.Count > Maximo)
                _visibles.Sort((a, b) => (a.transform.position - desde).sqrMagnitude.CompareTo((b.transform.position - desde).sqrMagnitude));
        }
        for (int i = 0; i < Maximo; i++)
        {
            bool hay = i < _visibles.Count;
            if (_raiz[i].gameObject.activeSelf != hay) _raiz[i].gameObject.SetActive(hay);
            if (hay) Pintar(i, _visibles[i], cam);
        }
    }

    void Pintar(int i, Rotulo r, Camera cam)
    {
        var rt = _raiz[i];
        rt.position = cam.WorldToScreenPoint(r.Punto);
        // Más pequeños de lejos, sin bajar de un tamaño legible.
        float d = Vector3.Distance(cam.transform.position, r.Punto);
        rt.localScale = Vector3.one * Mathf.Clamp(9f / Mathf.Max(1f, d), 0.7f, 1.1f);

        _nombre[i].text = r.texto;
        _nombre[i].color = r.color;
        bool barra = r.tipo == Rotulo.Tipo.Enemigo && r.vida && (r.Golpeado || r.vida.actual < r.vida.maxima);
        _barra[i].transform.parent.gameObject.SetActive(barra);
        if (barra) _barra[i].fillAmount = r.vida.actual / (float)Mathf.Max(1, r.vida.maxima);
        bool sub = !barra && !string.IsNullOrEmpty(r.subtitulo);
        _sub[i].gameObject.SetActive(sub);
        if (sub) _sub[i].text = r.subtitulo;
        // Sin subtítulo ni barra, el nombre baja para quedar pegado a la cabeza.
        _nombre[i].rectTransform.anchoredPosition = new Vector2(0f, sub || barra ? 26f : 4f);
        _novedad[i].gameObject.SetActive(r.novedad);
        if (r.novedad) _novedad[i].rectTransform.anchoredPosition = new Vector2(0f, (sub || barra ? 78f : 56f) + Mathf.Sin(Time.time * 4f) * 5f);
    }
}
