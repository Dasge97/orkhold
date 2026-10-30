using UnityEngine;
using UnityEngine.UI;

/// Recolocar a mano (08-formato-y-pvp.md §3.2): pasar recursos entre el cofre central y el escondite tocado.
/// Se trabaja sobre una copia del diseño; "Guardar" la manda a la API (PUT /fuerte/escondites), que comprueba
/// que no se crea ni se pierde nada y que todo cabe.
public class PantallaReparto : MonoBehaviour
{
    public static PantallaReparto Abierta { get; private set; }

    DisenoFuerte _copia;
    DisenoFuerte.PiezaDiseno _escondite;
    CatalogoPiezas.Pieza _pieza;
    int _centralAntes;
    RectTransform _panel;
    GameObject _velo, _filas;
    Text _cabecera, _mensaje;
    System.Action _alGuardar;

    public string Mensaje => _mensaje ? _mensaje.text : "";

    public static PantallaReparto Abrir(int x, int z, System.Action alGuardar = null)
    {
        var cat = CatalogoPiezas.Instancia;
        var original = Partida.Actual.fuerte;
        var q = original.En(x, z);
        var p = q != null ? cat.Buscar(q.tipo) : null;
        if (p == null || p.categoria != CatalogoPiezas.Categoria.Escondite) { Avisos.Mostrar("Toca un cofre, un barril o un enterrado", 1.8f); return null; }
        if (Abierta) Destroy(Abierta.gameObject);
        var go = Ui.Lienzo("PantallaReparto", 820);
        var r = go.AddComponent<PantallaReparto>();
        Abierta = r;
        r._copia = original.Copia();
        r._escondite = r._copia.En(x, z);
        r._pieza = p;
        r._centralAntes = original.central.Unidades;
        r._alGuardar = alGuardar;
        r._panel = Ui.PanelCentrado(go.transform, new Vector2(1300f, 900f), out r._velo);
        Ui.Texto(r._panel, "Repartir", 58, new Vector2(0f, 380f), new Vector2(1200f, 80f), negrita: true).color = Ui.Dorado;
        r._cabecera = Ui.Texto(r._panel, "", 34, new Vector2(0f, 300f), new Vector2(1200f, 60f));
        r._mensaje = Ui.Texto(r._panel, "", 32, new Vector2(0f, -290f), new Vector2(1200f, 60f));
        Ui.Boton(r._panel, "Guardar", new Vector2(-200f, -380f), new Vector2(340f, 96f), Ui.Verde, r.Guardar, 42);
        Ui.Boton(r._panel, "Cancelar", new Vector2(200f, -380f), new Vector2(340f, 96f), Ui.Gris, r.Cerrar, 38);
        r.Pintar();
        return r;
    }

    void OnDestroy() { if (Abierta == this) Abierta = null; }
    public void Cerrar() => Destroy(gameObject);

    int CapacidadCentral => Mathf.Max(CatalogoPiezas.Instancia.capacidadCentral, _centralAntes);

    /// Pasa "n" de un recurso: positivo, del central al escondite; negativo, al revés. Devuelve lo que se movió.
    public int Mover(Recurso r, int n)
    {
        var central = _copia.central;
        var esc = _escondite.contenido;
        int m = n > 0
            ? Mathf.Min(n, central[r], _pieza.capacidad - esc.Unidades)
            : -Mathf.Min(-n, esc[r], CapacidadCentral - central.Unidades);
        if (m == 0) { _mensaje.text = n > 0 ? "No queda de eso en el cofre central o el escondite está lleno" : "No queda de eso aquí o el cofre central está lleno"; return 0; }
        central[r] -= m; esc[r] += m;
        _mensaje.text = "";
        Pintar();
        return m;
    }

    void Pintar()
    {
        _cabecera.text = $"{_pieza.nombre} ({_escondite.x},{_escondite.z}): {_escondite.contenido.Unidades}/{_pieza.capacidad}   ·   " +
                         $"cofre central: {_copia.central.Unidades}/{CapacidadCentral}";
        if (_filas) Destroy(_filas);
        _filas = new GameObject("Filas", typeof(RectTransform));
        _filas.transform.SetParent(_panel, false);
        var t = _filas.transform;
        Ui.Texto(t, "Cofre central", 30, new Vector2(-330f, 220f), new Vector2(300f, 50f)).color = new Color(1f, 1f, 1f, 0.7f);
        Ui.Texto(t, _pieza.nombre, 30, new Vector2(330f, 220f), new Vector2(300f, 50f)).color = new Color(1f, 1f, 1f, 0.7f);
        for (int i = 0; i < Contenido.Recursos.Length; i++)
        {
            var r = Contenido.Recursos[i];
            float y = 150f - i * 85f;
            Ui.Texto(t, $"{Nombres.Plural(r, 2)}: {_copia.central[r]}", 34, new Vector2(-330f, y), new Vector2(320f, 70f));
            Ui.Boton(t, "← 5", new Vector2(-95f, y), new Vector2(110f, 70f), Ui.Gris, () => Mover(r, -5), 30);
            Ui.Boton(t, "← 1", new Vector2(15f, y), new Vector2(90f, 70f), Ui.Gris, () => Mover(r, -1), 30);
            Ui.Boton(t, "1 →", new Vector2(115f, y), new Vector2(90f, 70f), Ui.Gris, () => Mover(r, 1), 30);
            Ui.Boton(t, "5 →", new Vector2(225f, y), new Vector2(110f, 70f), Ui.Gris, () => Mover(r, 5), 30);
            Ui.Texto(t, $"{_escondite.contenido[r]}", 34, new Vector2(400f, y), new Vector2(160f, 70f));
        }
    }

    public void Guardar()
    {
        _mensaje.text = "Guardando...";
        Sincronizacion.EnviarReparto(_copia, (ok, error) =>
        {
            if (!this) return;
            if (!ok) { _mensaje.text = error; return; }
            Avisos.Mostrar("Escondites recolocados", 1.5f);
            _alGuardar?.Invoke();
            Cerrar();
        });
    }
}
