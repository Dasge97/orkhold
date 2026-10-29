using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Fase 2: vida, recolección, daño visible, muerte, botón con contexto y sonidos.
public class PruebaCombate
{
    [SetUp]
    public void Limpiar() { Partida.Reemplazar(new DatosPartida()); }

    static IEnumerator Atacar()
    {
        EntradaJuego.AtaquePantalla = true;
        yield return new WaitForSeconds(0.75f);
    }

    static IEnumerator Interactuar()
    {
        EntradaJuego.InteractuarPantalla = true;
        yield return new WaitForSeconds(0.9f);
    }

    [UnityTest]
    public IEnumerator GolpearArbolDaUnaMadera()
    {
        yield return Ayudas.Cargar("Pueblo");
        var j = Ayudas.Jugador();
        var arbol = Object.FindObjectsByType<FuenteRecurso>()
            .Where(f => f.recurso == Recurso.Madera)
            .OrderBy(f => Vector3.Distance(f.transform.position, j.transform.position)).FirstOrDefault();
        Assert.IsNotNull(arbol, "No hay árboles que se puedan talar en el pueblo");
        float escala = arbol.transform.localScale.x;

        // Al lado del árbol, mirando hacia otro lado: el ataque se gira solo.
        GestorEscena.Colocar(j.transform, arbol.transform.position + Vector3.back * 0.75f, 180f);
        yield return null;
        EntradaJuego.AtaquePantalla = true;
        yield return new WaitForSeconds(0.3f);
        Ayudas.Foto("40-golpe-arbol");
        yield return new WaitForSeconds(0.45f);
        Assert.AreEqual(1, arbol.Golpes, "El primer golpe no le llegó al árbol");
        yield return Atacar();
        yield return Atacar();
        Assert.IsTrue(arbol.Agotado, "Tras 3 golpes el árbol debería quedar agotado");
        yield return new WaitForSeconds(0.4f);
        Assert.Less(arbol.transform.localScale.x, escala * 0.5f, "El árbol agotado no encoge");

        Ayudas.Foto("41-arbol-agotado-y-madera");
        var madera = Object.FindObjectsByType<Recogible>().FirstOrDefault(r => r.recurso == Recurso.Madera);
        Assert.IsNotNull(madera, "El árbol no soltó madera");
        Assert.Less(Ayudas.DistanciaPlana(madera.transform.position, j.transform.position), 1.3f, "La madera cayó lejos del jugador");
        yield return Interactuar();
        Assert.AreEqual(1, Partida.Inventario.madera, "No hay 1 madera en el inventario");

        // Agotado: no admite más golpes.
        yield return Atacar();
        Assert.AreEqual(0, arbol.Golpes);
    }

    [UnityTest]
    public IEnumerator RegeneraFueraDeCombateYSeCuraEnLaFogata()
    {
        yield return Ayudas.Cargar("Pueblo");
        var j = Ayudas.Jugador();
        var reg = j.GetComponent<Regeneracion>();
        reg.cadaSegundos = 0.5f; reg.sinCombateSegundos = 0.3f;
        GestorEscena.Colocar(j.transform, j.transform.position + Vector3.back * 4f, 0f);
        j.Vida.Danar(3, j.transform.position + Vector3.forward);
        Assert.AreEqual(3, j.Vida.actual);
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(3, j.Vida.actual, "Regenera en pleno combate");
        yield return new WaitForSeconds(1.0f);
        Assert.Greater(j.Vida.actual, 3, "No regenera fuera de combate");

        reg.enabled = false;
        j.Vida.Danar(2);
        var fogata = Object.FindAnyObjectByType<Fogata>();
        Assert.IsNotNull(fogata, "El pueblo no tiene fogata");
        GestorEscena.Colocar(j.transform, fogata.transform.position + Vector3.back * 1.2f, 0f);
        yield return null; yield return null;
        Assert.AreEqual(j.Vida.maxima, j.Vida.actual, "La fogata no cura del todo");
    }

    [UnityTest]
    public IEnumerator ElGolpeadoParpadeaEnRojoYSaleUnNumero()
    {
        yield return Ayudas.Cargar("Pueblo");
        var orco = Object.FindAnyObjectByType<Enemigo>();
        var efecto = orco.GetComponent<EfectoDano>();
        Assert.IsNotNull(efecto, "El orco no tiene EfectoDano");
        orco.RecibirGolpe(1, orco.transform.position + Vector3.forward, null);
        Assert.IsTrue(efecto.Parpadeando, "No parpadea al recibir el golpe");
        GestorEscena.Colocar(Ayudas.Jugador().transform, orco.transform.position + Vector3.back * 1.5f, 0f);
        yield return null;
        Ayudas.Foto("42-orco-golpeado");
        Assert.IsTrue(Object.FindObjectsByType<NumeroFlotante>().Length > 0, "No sale el número de daño");
        yield return new WaitForSeconds(EfectoDano.DuracionParpadeo + 0.1f);
        Assert.IsFalse(efecto.Parpadeando, "El parpadeo dura más de 0,15 s");
    }

    [UnityTest]
    public IEnumerator AlMorirEnElPuebloReapareceEnLaFogata()
    {
        yield return Ayudas.Cargar("Pueblo");
        var j = Ayudas.Jugador();
        GestorEscena.Colocar(j.transform, j.transform.position + new Vector3(-6f, 0f, -6f), 0f);
        j.Vida.Danar(99);
        Assert.IsTrue(j.Vida.Muerto);
        yield return new WaitForSeconds(1.5f);
        Assert.IsTrue(Transicion.Ocupada, "No se oscurece la pantalla al morir");
        float t = 0f;
        while (Transicion.Ocupada && t < 6f) { t += Time.deltaTime; yield return null; }
        Assert.AreEqual("Pueblo", SceneManager.GetActiveScene().name);
        Assert.AreEqual(j.Vida.maxima, j.Vida.actual, "No revive con la vida llena");
        Assert.Less(Ayudas.DistanciaPlana(j.transform.position, GestorEscena.Actual.aparicion), 0.3f, "No reaparece en la fogata");
    }

    [UnityTest]
    public IEnumerator AlMorirEnLaMazmorraPierdeLaMitadDelBotin()
    {
        Partida.Sumar(Recurso.Moneda, 10);
        yield return Ayudas.Cargar("Mazmorra1");
        Partida.Sumar(Recurso.Moneda, 5);
        Partida.Sumar(Recurso.Hueso, 2);
        Ayudas.Jugador().Vida.Danar(99);
        yield return Ayudas.EsperarEscena("Pueblo", 10f);
        Assert.AreEqual(10 + 3, Partida.Inventario.monedas, "De 5 monedas ganadas debe conservar 3 (pierde la mitad, redondeando a su favor)");
        Assert.AreEqual(1, Partida.Inventario.hueso);
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, Ayudas.Puerta("mazmorra1").llegada), 0.3f,
            "No vuelve junto a la puerta de la mazmorra");
    }

    [UnityTest]
    public IEnumerator ElBotonDeInteractuarCambiaDeIcono()
    {
        yield return Ayudas.Cargar("Pueblo");
        var boton = Object.FindAnyObjectByType<BotonContexto>();
        Assert.IsNotNull(boton);
        var j = Ayudas.Jugador();
        var cofre = Ayudas.Puerta("asalto");
        GestorEscena.Colocar(j.transform, cofre.llegada, 0f);
        yield return new WaitForSeconds(0.25f);
        Assert.AreEqual(Interactuable.Icono.Cofre, boton.Actual);

        var moneda = Object.FindObjectsByType<Recogible>().First(r => r.recurso == Recurso.Moneda);
        GestorEscena.Colocar(j.transform, moneda.transform.position + Vector3.back * 0.6f, 0f);
        yield return new WaitForSeconds(0.25f);
        Assert.AreEqual(Interactuable.Icono.Recoger, boton.Actual);

        GestorEscena.Colocar(j.transform, new Vector3(5f, 0f, 5f), 0f);
        yield return new WaitForSeconds(0.25f);
        Assert.IsNull(boton.Actual, "Sin nada cerca no debería haber icono de contexto");
    }

    [Test]
    public void TodosLosSonidosTienenClips()
    {
        foreach (var g in Sonidos.NombresGrupos)
            Assert.Greater(Sonidos.Clips(g), 0, "Sin clips para " + g);
    }
}
