using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 9.2: cinco carteles en el pueblo; su texto sale al acercarse y se va al alejarse.
public class PruebaTutorial
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; }

    [UnityTest]
    public IEnumerator ElPuebloTieneCincoCartelesQueSeLeenAlAcercarse()
    {
        yield return Ayudas.Cargar("Pueblo");
        var carteles = Object.FindObjectsByType<Cartel>().OrderBy(c => c.name).ToArray();
        Assert.AreEqual(5, carteles.Length, "El pueblo debería tener 5 carteles");
        Assert.IsTrue(carteles.All(c => c.texto.Length > 20), "Hay carteles sin texto");
        // El primero está junto a la fogata, a menos de 4 m de donde se aparece.
        Assert.Less(Ayudas.DistanciaPlana(carteles[0].transform.position, GestorEscena.Actual.aparicion), 4f, "El cartel 1 está lejos de la aparición");

        var j = Ayudas.Jugador();
        var c3 = carteles[2];
        GestorEscena.Colocar(j.transform, c3.transform.position + Vector3.back * 1.2f, 0f);
        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual(c3.texto, Cartel.Visible, "Al acercarse no sale el texto del cartel");
        Ayudas.Foto("tutorial-cartel");

        GestorEscena.Colocar(j.transform, c3.transform.position + Vector3.back * 6f, 0f);
        yield return new WaitForSeconds(0.6f);
        Assert.AreEqual("", Cartel.Visible, "Al alejarse el texto sigue en pantalla");
    }
}
