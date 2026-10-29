using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 4: inventario, catálogo de piezas, modo construir, piezas del fuerte, guardado local y menú.
public class PruebaFuerte
{
    string _ruta;

    [SetUp]
    public void Preparar()
    {
        _ruta = Path.Combine(Application.temporaryCachePath, "prueba-partida.json");
        if (File.Exists(_ruta)) File.Delete(_ruta);
        Guardado.Ruta = _ruta;
        Guardado.Activo = true;
        Partida.Reemplazar(new DatosPartida());
        Fuerte.DisenoAsalto = null;
    }

    [TearDown]
    public void Recoger() { Guardado.Ruta = Guardado.RutaPorDefecto; }

    static IEnumerator EntrarAConstruir()
    {
        yield return Ayudas.Cargar("Fuerte");
        var ed = Object.FindAnyObjectByType<EditorFuerte>();
        Assert.IsNotNull(ed, "La escena Fuerte no tiene EditorFuerte");
        ed.Entrar();
        yield return null;
    }

    static EditorFuerte Editor => Object.FindAnyObjectByType<EditorFuerte>();

    [UnityTest]
    public IEnumerator CincoMurosSiguenAhiAlVolverAAbrir()
    {
        Partida.Sumar(Recurso.Madera, 10);
        yield return EntrarAConstruir();
        var ed = Editor;
        ed.Seleccionar("muro_madera");
        for (int x = 3; x <= 7; x++) Assert.IsTrue(ed.TocarCasilla(x, 3), $"No se pudo poner el muro en ({x},3): {ed.UltimoMotivo}");
        Assert.AreEqual(0, Partida.Inventario.madera, "Cada muro de madera cuesta 2");
        Assert.IsFalse(ed.TocarCasilla(8, 3), "Sin madera no debería poder poner otro");
        StringAssert.Contains("madera", ed.UltimoMotivo);
        yield return null;
        Ayudas.Foto("46-construir-cinco-muros");
        ed.Salir();
        Assert.IsTrue(File.Exists(_ruta), "Listo no guarda la partida");

        // Cerrar la app y volver a abrir: la partida sale del disco.
        Partida.Reemplazar(new DatosPartida());
        Guardado.Cargar();
        Assert.AreEqual(5, Partida.Actual.fuerte.piezas.Count);
        Assert.AreEqual(0, Partida.Inventario.madera);
        yield return Ayudas.Cargar("Fuerte");
        yield return null;
        Assert.AreEqual(5, Fuerte.Actual.Piezas.Count(p => p && p.tipo == "muro_madera"), "Al volver no están los 5 muros");
    }

    [UnityTest]
    public IEnumerator TocarLaPantallaPoneLaPiezaEnLaCasillaTocada()
    {
        Partida.Sumar(Recurso.Piedra, 3);
        yield return EntrarAConstruir();
        var ed = Editor;
        ed.Seleccionar("muro_piedra");
        var punto = Camera.main.WorldToScreenPoint(Fuerte.Actual.Mundo(10, 12));
        Assert.IsTrue(ed.TocarPantalla(punto), ed.UltimoMotivo);
        Assert.IsNotNull(Partida.Actual.fuerte.En(10, 12));
    }

    [UnityTest]
    public IEnumerator QuitarDevuelveLoQueCosto()
    {
        Partida.Sumar(Recurso.Madera, 2);
        Partida.Sumar(Recurso.Hierro, 1);
        yield return EntrarAConstruir();
        var ed = Editor;
        ed.Seleccionar("puerta");
        Assert.IsTrue(ed.TocarCasilla(5, 5));
        Assert.AreEqual(0, Partida.Inventario.madera);
        Assert.AreEqual(0, Partida.Inventario.hierro);
        Assert.IsTrue(ed.TocarCasilla(5, 5), "Tocar una pieza puesta debería quitarla");
        Assert.AreEqual(2, Partida.Inventario.madera);
        Assert.AreEqual(1, Partida.Inventario.hierro);
        Assert.AreEqual(0, Partida.Actual.fuerte.piezas.Count);
    }

    [UnityTest]
    public IEnumerator SeRespetanLosLimitesYLasCasillasProhibidas()
    {
        Partida.Sumar(Recurso.Hierro, 100);
        Partida.Sumar(Recurso.Moneda, 100);
        yield return EntrarAConstruir();
        var ed = Editor;
        var mapa = Fuerte.Actual.Mapa;
        var cofre = ReglasFuerte.Cofre(mapa);
        var llegada = ReglasFuerte.Llegada(mapa);
        ed.Seleccionar("trampa");
        Assert.IsFalse(ed.TocarCasilla(cofre.x, cofre.y), "No se construye sobre el cofre");
        Assert.IsFalse(ed.TocarCasilla(llegada.x, llegada.y), "No se construye en la entrada");
        Assert.IsFalse(ed.TocarCasilla(0, 0), "No se construye sobre la valla");
        for (int i = 0; i < 6; i++) Assert.IsTrue(ed.TocarCasilla(2 + i, 10), ed.UltimoMotivo);
        Assert.IsFalse(ed.TocarCasilla(9, 10), "Límite de 6 trampas");
        StringAssert.Contains("6", ed.UltimoMotivo);

        ed.Seleccionar("orco_defensor");
        for (int i = 0; i < 4; i++) Assert.IsTrue(ed.TocarCasilla(2 + i, 12), ed.UltimoMotivo);
        Assert.IsFalse(ed.TocarCasilla(7, 12), "Límite de 4 defensores");
        Assert.AreEqual(100 - 40, Partida.Inventario.monedas, "El orco defensor cuesta 10 monedas");
        yield return null;
        Ayudas.Foto("47-construir-trampas-y-defensores");
    }

    [UnityTest]
    public IEnumerator LosDefensoresSeMejoranConHueso()
    {
        Partida.Sumar(Recurso.Moneda, 15);
        Partida.Sumar(Recurso.Hueso, 10);
        yield return EntrarAConstruir();
        var ed = Editor;
        ed.Seleccionar("esqueleto_defensor");
        Assert.IsTrue(ed.TocarCasilla(4, 4));
        ed.Seleccionar(EditorFuerte.Mejorar);
        for (int i = 0; i < 3; i++) Assert.IsTrue(ed.TocarCasilla(4, 4), ed.UltimoMotivo);
        Assert.IsFalse(ed.TocarCasilla(4, 4), "La mejora máxima es +3");
        Assert.AreEqual(3, Partida.Actual.fuerte.En(4, 4).vida_extra);
        Assert.AreEqual(10 - 6, Partida.Inventario.hueso, "Cada mejora cuesta 2 huesos");
        var defensor = Fuerte.Actual.Piezas.First(p => p.tipo == "esqueleto_defensor");
        Assert.AreEqual(4 + 3, defensor.vida.maxima);
    }

    [UnityTest]
    public IEnumerator ElOrcoJefeGastaLaUnidadDelBotin()
    {
        yield return EntrarAConstruir();
        var ed = Editor;
        ed.Seleccionar("orco_jefe");
        Assert.IsFalse(ed.TocarCasilla(6, 6), "Sin unidad no se puede poner");
        Partida.Actual.unidades.Add(new DatosUnidad { tipo = Jefe.TipoUnidad });
        Assert.IsTrue(ed.TocarCasilla(6, 6), ed.UltimoMotivo);
        Assert.AreEqual(0, Partida.Actual.unidades.Count);
        Assert.IsTrue(ed.TocarCasilla(6, 6));
        Assert.AreEqual(1, Partida.Actual.unidades.Count, "Al quitarlo vuelve al inventario");
    }

    [UnityTest]
    public IEnumerator LaPuertaPropiaSeAbreYLaDelAsaltoSeRompe()
    {
        Partida.Actual.fuerte.piezas.Add(new DisenoFuerte.PiezaDiseno { x = 3, z = 7, tipo = "puerta" });
        yield return Ayudas.Cargar("Fuerte");
        yield return null;
        var puerta = Fuerte.Actual.Piezas.First(p => p.esPuerta);
        GestorEscena.Colocar(Ayudas.Jugador().transform, puerta.transform.position + Vector3.left * 0.9f, 90f);
        yield return null; yield return null;
        Assert.IsTrue(puerta.Abierta, "La puerta propia no se abre al acercarse");

        Fuerte.DisenoAsalto = new DisenoFuerte();
        Fuerte.DisenoAsalto.piezas.Add(new DisenoFuerte.PiezaDiseno { x = 3, z = 7, tipo = "muro_madera" });
        int rotos = 0;
        System.Action<PiezaFuerte> contar = _ => rotos++;
        PiezaFuerte.AlRomper += contar;
        yield return Ayudas.Cargar("Asalto");
        yield return null;
        var muro = Fuerte.Actual.Piezas.First();
        Assert.AreEqual(3, muro.vida.maxima);
        for (int i = 0; i < 3; i++) muro.RecibirGolpe(1, muro.transform.position + Vector3.left, null);
        yield return null;
        PiezaFuerte.AlRomper -= contar;
        Assert.AreEqual(1, rotos, "El muro de madera se rompe en 3 golpes");
        Assert.IsTrue(muro == null, "El muro roto sigue en la escena");
    }

    [UnityTest]
    public IEnumerator ElPuebloEnseñaElFuerteEnElHueco()
    {
        for (int i = 0; i < 3; i++) Partida.Actual.fuerte.piezas.Add(new DisenoFuerte.PiezaDiseno { x = 4 + i, z = 9, tipo = "muro_piedra" });
        yield return Ayudas.Cargar("Pueblo");
        yield return null;
        var vista = Object.FindObjectsByType<Fuerte>().FirstOrDefault(f => f.modo == PiezaFuerte.Modo.Vista);
        Assert.IsNotNull(vista, "El pueblo no tiene la vista del fuerte");
        Assert.AreEqual(3, vista.Piezas.Count);
    }

    [UnityTest]
    public IEnumerator UnFicheroRotoEmpiezaPartidaNueva()
    {
        File.WriteAllText(_ruta, "{ esto no es json");
        Guardado.Cargar();
        Assert.AreEqual(0, Partida.Inventario.monedas);
        Assert.IsTrue(File.Exists(_ruta + ".roto"), "No aparta el fichero roto");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ElMenuReiniciaLaPartidaYCambiaAjustes()
    {
        Partida.Sumar(Recurso.Moneda, 30);
        yield return Ayudas.Cargar("Pueblo");
        var menu = Object.FindAnyObjectByType<Menu>();
        Assert.IsNotNull(menu, "La interfaz no tiene menú");
        menu.Abrir();
        Assert.IsTrue(menu.Abierto);
        yield return null;
        bool sonido = Ajustes.Sonido;
        Ajustes.Sonido = !sonido;
        Assert.AreEqual(!sonido, Sonidos.Activo);
        Ajustes.Sonido = sonido;
        menu.Reiniciar();
        Assert.AreEqual(0, Partida.Inventario.monedas);
        yield return Ayudas.EsperarEscena("Pueblo");
    }
}
