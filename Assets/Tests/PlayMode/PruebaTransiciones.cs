using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 1: se entra y se sale de la mazmorra, del fuerte y del asalto con transición,
/// y al volver al pueblo se aparece junto a la puerta por la que se salió.
public class PruebaTransiciones
{
    [UnityTest]
    public IEnumerator EntrarYSalirDeLaMazmorra()
    {
        yield return Ayudas.Cargar("Pueblo");
        yield return Ayudas.Pisar(Ayudas.Puerta("mazmorra1"));
        Assert.IsTrue(Transicion.Ocupada, "Pisar la puerta no empezó la transición");
        yield return Ayudas.EsperarEscena("Mazmorra1");
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, GestorEscena.Actual.aparicion), 0.3f,
            "En la mazmorra no se aparece en la entrada");

        yield return Ayudas.Pisar(Ayudas.Puerta("salida"));
        yield return Ayudas.EsperarEscena("Pueblo");
        var d = Ayudas.Puerta("mazmorra1");
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, d.llegada), 0.3f,
            "Al volver no se aparece junto a la puerta de la mazmorra");
        // Recién llegado junto a la puerta no debe volver a entrar solo.
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("Pueblo", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator EntrarYSalirDelFuerte()
    {
        yield return Ayudas.Cargar("Pueblo");
        yield return Ayudas.Pisar(Ayudas.Puerta("fuerte"));
        yield return Ayudas.EsperarEscena("Fuerte");
        yield return Ayudas.Pisar(Ayudas.Puerta("salida"));
        yield return Ayudas.EsperarEscena("Pueblo");
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, Ayudas.Puerta("fuerte").llegada), 0.3f);
    }

    [UnityTest]
    public IEnumerator AsaltoDesdeElCofre()
    {
        yield return Ayudas.Cargar("Pueblo");
        var cofre = Ayudas.Puerta("asalto");
        Assert.IsFalse(cofre.alPisar, "El cofre PvP se usa con el botón, no pisándolo");
        GestorEscena.Colocar(Ayudas.Jugador().transform, cofre.llegada, 0f);
        yield return null;
        Assert.AreEqual(cofre, Interactuable.MasCercano(Ayudas.Jugador().transform.position), "El cofre no es lo más cercano para interactuar");
        EntradaJuego.InteractuarPantalla = true;
        yield return null;
        yield return null;
        var buscar = cofre.GetComponent<BuscarRival>();
        Assert.IsTrue(buscar.Abierto, "El cofre PvP no abre el panel de buscar rival");
        buscar.Elegir(0);
        yield return Ayudas.EsperarEscena("Asalto");
        // Salir por la entrada es rendirse: sale el resultado y se vuelve con su botón.
        yield return Ayudas.Pisar(Ayudas.Puerta("salida"));
        Assert.AreEqual(Asalto.Fin.Abandonado, Asalto.Actual.Estado);
        Asalto.Actual.Volver();
        yield return Ayudas.EsperarEscena("Pueblo");
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, Ayudas.Puerta("asalto").transform.position), 1.5f);
    }
}
