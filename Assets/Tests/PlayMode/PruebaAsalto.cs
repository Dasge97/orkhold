using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 5: reglas del asalto, pantalla de resultado, fuertes de prueba y partidas del jugador automático.
public class PruebaAsalto
{
    [SetUp]
    public void Preparar()
    {
        Guardado.Activo = false;
        Partida.Reemplazar(new DatosPartida());
        Time.timeScale = 1f;
    }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; Time.timeScale = 1f; EntradaJuego.MoverPantalla = Vector2.zero; }

    static IEnumerator Asaltar(int fuerte)
    {
        Fuerte.DisenoAsalto = DisenoFuerte.DeJson(Catalogo.Instancia.fuertesPrueba[fuerte].text);
        yield return Ayudas.Cargar("Asalto");
        yield return null;
    }

    [Test]
    public void LosTresFuertesDePruebaCumplenLasReglas()
    {
        var cat = CatalogoPiezas.Instancia;
        var mapa = CargadorMapa.Leer(File.ReadAllText("Assets/Mapas/fuerte-base.txt"));
        Assert.AreEqual(3, Catalogo.Instancia.fuertesPrueba.Length);
        foreach (var t in Catalogo.Instancia.fuertesPrueba)
        {
            var d = DisenoFuerte.DeJson(t.text);
            Assert.Greater(d.piezas.Count, 0, t.name);
            foreach (var p in d.piezas)
            {
                Assert.IsNotNull(cat.Buscar(p.tipo), $"{t.name}: tipo {p.tipo}");
                Assert.IsTrue(ReglasFuerte.CasillaConstruible(mapa, p.x, p.z), $"{t.name}: casilla ({p.x},{p.z})");
            }
            foreach (CatalogoPiezas.Categoria k in System.Enum.GetValues(typeof(CatalogoPiezas.Categoria)))
                Assert.LessOrEqual(d.Contar(p => cat.Buscar(p.tipo).categoria == k), cat.Limite(k), $"{t.name}: límite de {k}");
        }
    }

    [UnityTest]
    public IEnumerator TocarElCofreGanaYDaRecompensas()
    {
        yield return Asaltar(0);
        var a = Asalto.Actual;
        Assert.IsNotNull(a, "La escena Asalto no tiene Asalto");
        Assert.AreEqual(90f, a.duracion);
        Assert.Greater(Fuerte.Actual.Piezas.Count, 0, "No se levantaron las piezas del rival");
        GestorEscena.Colocar(Ayudas.Jugador().transform, a.PosicionCofre + Vector3.left * 0.8f, 90f);
        yield return null; yield return null;
        Assert.AreEqual(Asalto.Fin.Ganado, a.Estado);
        Assert.AreEqual(10, Partida.Inventario.monedas);
        Assert.AreEqual(2, Partida.Inventario.madera + Partida.Inventario.piedra + Partida.Inventario.hierro + Partida.Inventario.hueso);
        Assert.AreEqual(1020, Partida.Actual.puntuacion);
        Assert.IsTrue(Object.FindAnyObjectByType<PantallaAsalto>().ResultadoVisible, "No sale la pantalla de resultado");
        yield return null;
        Ayudas.Foto("48-asalto-ganado");
    }

    [UnityTest]
    public IEnumerator SeAcabaElTiempoYPierde()
    {
        yield return Asaltar(0);
        var a = Asalto.Actual;
        a.duracion = 1f;
        yield return new WaitForSeconds(1.3f);
        Assert.AreEqual(Asalto.Fin.PerdidoTiempo, a.Estado);
        Assert.AreEqual(3, Partida.Inventario.monedas);
        Assert.AreEqual(985, Partida.Actual.puntuacion);
    }

    [UnityTest]
    public IEnumerator MorirPierde()
    {
        yield return Asaltar(0);
        Ayudas.Jugador().Vida.Danar(99);
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(Asalto.Fin.PerdidoMuerte, Asalto.Actual.Estado);
    }

    [UnityTest]
    public IEnumerator CuentaLosMurosRotosYLaBarraAvanza()
    {
        yield return Asaltar(0);
        var a = Asalto.Actual;
        Assert.AreEqual(0f, 1f - a.Distancia / a.DistanciaInicial, 0.05f, "Al empezar la barra debería estar vacía");
        var muro = Fuerte.Actual.Piezas.First(p => p.tipo == "muro_madera");
        for (int i = 0; i < muro.vida.maxima; i++) muro.RecibirGolpe(1, muro.transform.position + Vector3.left, null);
        yield return null;
        Assert.AreEqual(1, a.MurosRotos);
        GestorEscena.Colocar(Ayudas.Jugador().transform, a.PosicionCofre + Vector3.left * 2f, 90f);
        yield return null;
        Assert.Greater(1f - a.Distancia / a.DistanciaInicial, 0.5f);
        Ayudas.Foto("49-asalto-en-curso");
    }

    // ---------- partidas del jugador automático (tarea 5.5) ----------

    static readonly string Informe = Path.Combine("Revision", "asaltos.txt");

    static IEnumerator Jugar(int fuerte, bool bueno, System.Action<Asalto.Resultado> fin)
    {
        yield return Asaltar(fuerte);
        Time.timeScale = 3f;
        var bot = new GameObject("Bot").AddComponent<BotAsalto>();
        bot.bueno = bueno;
        Asalto.Resultado r = null;
        System.Action<Asalto.Resultado> f = x => r = x;
        Asalto.AlTerminar += f;
        while (r == null) yield return null;
        Asalto.AlTerminar -= f;
        Time.timeScale = 1f;
        int retiradas = bot.Retiradas;
        Object.Destroy(bot.gameObject);
        EntradaJuego.CorrerPantalla = false;
        File.AppendAllText(Informe, $"{BuscarRival.NombresPrueba[fuerte],-8} {(bueno ? "bueno" : "malo "),-6} {(r.ganado ? "GANA " : "pierde")} " +
                                    $"{r.segundos,3} s  muros rotos {r.murosRotos,2}  retiradas {retiradas}  ({r.motivo})\n");
        fin(r);
    }

    [UnityTest, Timeout(600000)]
    public IEnumerator JugadorAutomaticoContraLosTresFuertes()
    {
        Directory.CreateDirectory("Revision");
        File.WriteAllText(Informe, "fuerte   juego  resultado\n");
        var ganadosBueno = new int[3];
        var segundosBueno = new System.Collections.Generic.List<int>[3];
        var malo = new Asalto.Resultado[3];
        for (int f = 0; f < 3; f++)
        {
            segundosBueno[f] = new System.Collections.Generic.List<int>();
            for (int i = 0; i < (f == 2 ? 5 : 3); i++)
                yield return Jugar(f, true, r => { if (r.ganado) { ganadosBueno[f]++; segundosBueno[f].Add(r.segundos); } });
            int ff = f;
            yield return Jugar(f, false, r => malo[ff] = r);
        }
        // Criterio de la tarea 5.5: el difícil se gana en ~60 s jugando bien y se pierde jugando mal.
        Assert.Greater(ganadosBueno[2], 0, "Jugando bien no se gana nunca el fuerte difícil");
        float media = (float)segundosBueno[2].Average();
        Assert.That(media, Is.InRange(35f, 85f), $"El difícil se gana en {media:F0} s de media (objetivo ~60 s)");
        Assert.IsFalse(malo[2].ganado, "Jugando mal se gana el fuerte difícil");
        Assert.AreEqual(3, ganadosBueno[0], "Jugando bien se pierde el fácil");
    }
}
