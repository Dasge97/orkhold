using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// Informe de un saqueo recibido (08-formato-y-pvp.md §4.5): sobre la rejilla de tu fuerte, qué rompió,
/// qué miró y qué marcó el atacante, y qué encontró. Sale al entrar en tu fuerte si hay informes sin ver;
/// en el pueblo, solo un aviso.
public class PantallaInforme : MonoBehaviour
{
    public static PantallaInforme Abierta { get; private set; }

    readonly List<MensajesApi.Informe> _informes = new List<MensajesApi.Informe>();
    int _i;
    RectTransform _panel;
    GameObject _velo, _contenido;

    public int Total => _informes.Count;
    public string Texto { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        Sincronizacion.AlLlegarInformes += () =>
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Fuerte") Abrir(Sincronizacion.InformesNuevos);
            else Avisos.Mostrar("Rook: \"Anoche entraron en tu parcela.\" Mira el informe en tu fuerte", 3.5f);
        };
    }

    public static PantallaInforme Abrir(List<MensajesApi.Informe> informes)
    {
        if (informes == null || informes.Count == 0) return null;
        if (Abierta) Destroy(Abierta.gameObject);
        var go = Ui.Lienzo("PantallaInforme", 850);
        var p = go.AddComponent<PantallaInforme>();
        Abierta = p;
        p._informes.AddRange(informes);
        p._panel = Ui.PanelCentrado(go.transform, new Vector2(1600f, 960f), out p._velo);
        p.Mostrar(0);
        return p;
    }

    void OnDestroy() { if (Abierta == this) Abierta = null; }

    public void Cerrar()
    {
        Sincronizacion.InformesVistos();
        Destroy(gameObject);
    }

    public void Mostrar(int i)
    {
        _i = Mathf.Clamp(i, 0, _informes.Count - 1);
        if (_contenido) Destroy(_contenido);
        _contenido = new GameObject("Contenido", typeof(RectTransform));
        _contenido.transform.SetParent(_panel, false);
        var c = _contenido.transform;
        var inf = _informes[_i];
        Ui.Texto(c, "Han saqueado tu fuerte", 56, new Vector2(0f, 420f), new Vector2(1500f, 80f), negrita: true).color = Ui.Dorado;
        Rejilla(c, inf, new Vector2(-380f, -20f), 46f);

        var sb = new StringBuilder();
        sb.AppendLine($"<b>{inf.atacante}</b> entró en tu parcela.");
        sb.AppendLine();
        int marcas = inf.marcas?.Count ?? 0;
        if (inf.encontrado > 0) sb.AppendLine($"Encontró algo en {inf.encontrado} de {marcas} escondites marcados y se llevó {MensajesApi.Texto(inf.botin)}.");
        else sb.AppendLine($"Marcó {marcas} sitios y no encontró nada." + (inf.tasa > 0 ? $" Te quedas su tasa: {inf.tasa} monedas." : ""));
        sb.AppendLine($"Puntos: {(inf.puntos >= 0 ? "+" : "")}{inf.puntos}");
        sb.AppendLine();
        int rotos = inf.rotos?.Count ?? 0, miradas = inf.miradas?.Count ?? 0;
        sb.AppendLine($"Rompió {rotos} {(rotos == 1 ? "muro" : "muros")} y miró {miradas} {(miradas == 1 ? "cosa" : "cosas")}.");
        sb.AppendLine(inf.encontrado > 0 ? "Ese escondite ya está quemado: cambia lo que guardas de sitio." : "Tus escondites aguantaron.");
        Texto = sb.ToString();
        Ui.Texto(c, Texto, 32, new Vector2(400f, 90f), new Vector2(720f, 520f), TextAnchor.UpperLeft);
        Ui.Texto(c, "<color=#e05a4a>X</color> muro roto   <color=#9ad0ff>o</color> mirado   <color=#ffd34d>B</color> botín   " +
                    "V vacío   <color=#ff8a80>×</color> sin camino", 28, new Vector2(400f, -250f), new Vector2(760f, 50f));
        if (_informes.Count > 1)
            Ui.Boton(c, $"Siguiente ({_i + 1}/{_informes.Count})", new Vector2(220f, -390f), new Vector2(380f, 90f), Ui.Gris, () => Mostrar((_i + 1) % _informes.Count), 34);
        Ui.Boton(c, "Entendido", new Vector2(600f, -390f), new Vector2(320f, 90f), Ui.Verde, Cerrar, 40);
    }

    /// La rejilla del fuerte propio con lo que hizo el atacante encima.
    static void Rejilla(Transform padre, MensajesApi.Informe inf, Vector2 centro, float lado)
    {
        var fuerte = Partida.Actual.fuerte;
        var cat = CatalogoPiezas.Instancia;
        const int N = 16;
        var raiz = Ui.Caja(padre, "Rejilla", new Color(0f, 0f, 0f, 0.3f), centro, new Vector2(N * lado + 8f, N * lado + 8f));
        var rotos = new HashSet<Vector2Int>();
        if (inf.rotos != null) foreach (var r in inf.rotos) rotos.Add(new Vector2Int(r.x, r.z));
        var miradas = new HashSet<Vector2Int>();
        if (inf.miradas != null) foreach (var m in inf.miradas) miradas.Add(new Vector2Int(m.x, m.z));
        var marcas = new Dictionary<Vector2Int, string>();
        if (inf.marcas != null) foreach (var m in inf.marcas) marcas[new Vector2Int(m.x, m.z)] = m.resultado;
        for (int x = 0; x < N; x++)
            for (int z = 0; z < N; z++)
            {
                var pos = new Vector2((x - (N - 1) / 2f) * lado, (z - (N - 1) / 2f) * lado);
                bool borde = x == 0 || z == 0 || x == N - 1 || z == N - 1;
                var q = fuerte.En(x, z);
                var p = q != null && cat ? cat.Buscar(q.tipo) : null;
                Color color = borde ? new Color(0.25f, 0.27f, 0.3f) : new Color(0.32f, 0.42f, 0.3f);
                if (x == 8 && z == 7) color = new Color(0.75f, 0.55f, 0.2f);
                else if (p != null && p.categoria == CatalogoPiezas.Categoria.Muro) color = new Color(0.55f, 0.55f, 0.58f);
                else if (p != null && p.categoria == CatalogoPiezas.Categoria.Escondite) color = new Color(0.6f, 0.42f, 0.25f);
                var celda = Ui.Caja(raiz, $"{x},{z}", color, pos, new Vector2(lado - 3f, lado - 3f));
                var k = new Vector2Int(x, z);
                string marca = "";
                if (rotos.Contains(k)) marca = "<color=#e05a4a>X</color>";
                if (miradas.Contains(k)) marca += "<color=#9ad0ff>o</color>";
                if (marcas.TryGetValue(k, out var res))
                    marca = res == "botin" ? "<color=#ffd34d>B</color>" : res == "vacio" ? "V" : res == "inalcanzable" ? "<color=#ff8a80>×</color>" : "·";
                if (marca.Length > 0) Ui.Texto(celda, marca, 30, Vector2.zero, new Vector2(lado, lado), negrita: true);
            }
    }
}
