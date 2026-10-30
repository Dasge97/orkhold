using System.Collections.Generic;
using UnityEngine;

/// Pone en escena las piezas de un DisenoFuerte sobre fuerte-base, en el modo indicado.
public static class ConstructorFuerte
{
    public const string NombreContenedor = "Piezas del fuerte";

    /// "origen": la raíz del mapa fuerte-base (su casilla x=0, z=0 está en su origen).
    public static List<PiezaFuerte> Construir(DisenoFuerte diseno, Transform origen, PiezaFuerte.Modo modo)
    {
        Limpiar(origen);
        var lista = new List<PiezaFuerte>();
        var cat = CatalogoPiezas.Instancia;
        if (!cat || diseno == null) return lista;
        var contenedor = new GameObject(NombreContenedor).transform;
        contenedor.SetParent(origen, false);

        foreach (var q in diseno.piezas)
        {
            var p = cat.Buscar(q.tipo);
            if (p == null || !p.prefab) { Debug.LogWarning("Pieza desconocida en el diseño: " + q.tipo); continue; }
            // El enterrado solo lo ve su dueño (en el pueblo, el hueco del fuerte lo enseña a todos).
            if (q.tipo == "enterrado" && modo != PiezaFuerte.Modo.Propio) continue;
            var g = Object.Instantiate(p.prefab, contenedor);
            g.name = $"{p.id} ({q.x},{q.z})";
            g.transform.localPosition = ReglasFuerte.Celda(q.x, q.z);
            g.transform.localRotation = Quaternion.Euler(0f, p.categoria == CatalogoPiezas.Categoria.Muro ? GiroMuro(diseno, cat, q) : 180f, 0f);
            g.SetActive(true);
            lista.Add(Preparar(g, p, q, modo));
        }
        return lista;
    }

    public static void Limpiar(Transform origen)
    {
        var viejo = origen.Find(NombreContenedor);
        if (!viejo) return;
        viejo.SetParent(null);
        Object.Destroy(viejo.gameObject);
    }

    /// Los muros siguen la línea de sus vecinos: este-oeste (0°) o norte-sur (90°).
    static float GiroMuro(DisenoFuerte d, CatalogoPiezas cat, DisenoFuerte.PiezaDiseno q)
    {
        bool EsMuro(int x, int z) { var o = d.En(x, z); return o != null && cat.Buscar(o.tipo)?.categoria == CatalogoPiezas.Categoria.Muro; }
        bool ejeX = EsMuro(q.x - 1, q.z) || EsMuro(q.x + 1, q.z);
        bool ejeZ = EsMuro(q.x, q.z - 1) || EsMuro(q.x, q.z + 1);
        return ejeZ && !ejeX ? 90f : 0f;
    }

    static PiezaFuerte Preparar(GameObject g, CatalogoPiezas.Pieza p, DisenoFuerte.PiezaDiseno q, PiezaFuerte.Modo modo)
    {
        var pf = g.AddComponent<PiezaFuerte>();
        pf.tipo = p.id; pf.categoria = p.categoria; pf.modo = modo; pf.x = q.x; pf.z = q.z;
        pf.esPuerta = p.EsPuerta;
        pf.modelo = g.transform.childCount > 0 ? g.transform.GetChild(0) : null;

        switch (p.categoria)
        {
            case CatalogoPiezas.Categoria.Muro:
                if (!g.GetComponent<Collider>())
                {
                    var b = g.AddComponent<BoxCollider>();
                    b.size = new Vector3(1f, 1.1f, p.EsPuerta ? 0.35f : 1f); b.center = new Vector3(0f, 0.55f, 0f);
                }
                var v = g.GetComponent<Vida>();
                if (!v) v = g.AddComponent<Vida>();
                v.maxima = p.vida + q.vida_extra; v.actual = v.maxima;
                pf.vida = v;
                break;
            case CatalogoPiezas.Categoria.Trampa:
                var t = g.GetComponent<Trampa>();
                if (t) { t.oculta = modo == PiezaFuerte.Modo.Asalto; t.enabled = modo == PiezaFuerte.Modo.Asalto; }
                break;
            case CatalogoPiezas.Categoria.Escondite:
                // Cofres, barriles y doble fondo ocupan la casilla; el enterrado parece suelo.
                if (q.tipo != "enterrado" && !g.GetComponent<Collider>())
                {
                    var bc = g.AddComponent<BoxCollider>();
                    bc.size = new Vector3(0.8f, 0.9f, 0.8f); bc.center = new Vector3(0f, 0.45f, 0f);
                }
                if (modo == PiezaFuerte.Modo.Asalto && (q.tipo == "cofre" || q.tipo == "barril"))
                {
                    var m = g.AddComponent<Mirable>();
                    m.x = q.x; m.z = q.z; m.alcance = 1.4f;
                }
                break;
            case CatalogoPiezas.Categoria.Defensor:
                var e = g.GetComponent<Enemigo>();
                var vd = g.GetComponent<Vida>();
                if (vd) { vd.maxima = p.vida + q.vida_extra; vd.actual = vd.maxima; }
                pf.vida = vd;
                if (e)
                {
                    e.reaparecerSegundos = 0f;
                    e.monedasMin = e.monedasMax = 0; e.probabilidadExtra = 0f;
                    // Un defensor vigila todo su fuerte: ve más lejos y persigue por todo el recinto.
                    e.distanciaVer = Mathf.Max(e.distanciaVer, 8f);
                    e.distanciaPerder = 12f;
                    e.patrulla = false;
                    e.enabled = modo == PiezaFuerte.Modo.Asalto;
                    var jefe = e as Jefe;
                    if (jefe) jefe.deMazmorra = false;
                }
                break;
        }
        return pf;
    }
}
