using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 13 (07-lore.md §7): los carteles del tutorial los dicen personajes del pueblo, Gruk dice su frase,
/// Ferra tiene la forja y la pantalla de carga del pueblo dice "Vado Gris".
public class PruebaLore
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; }

    [UnityTest]
    public IEnumerator LosPersonajesDelPuebloHablan()
    {
        yield return Ayudas.Cargar("Pueblo");
        var carteles = Object.FindObjectsByType<Cartel>().OrderBy(c => c.name).ToArray();
        Assert.AreEqual(5, carteles.Length);
        string[] quien = { "Maela", "Rook", "Ferra", "Ansel", "Tobin" };
        for (int i = 0; i < 5; i++)
        {
            StringAssert.StartsWith(quien[i] + ":", carteles[i].texto, $"El cartel {i + 1} no lo dice {quien[i]}");
            Assert.IsNotNull(carteles[i].GetComponentInChildren<Animator>(), $"{quien[i]} no tiene modelo animado");
        }
        Assert.IsNotNull(carteles[2].GetComponent<Herreria>(), "Ferra no tiene la forja");

        var gruk = Object.FindObjectsByType<Npc>().FirstOrDefault(n => n.nombre == "Gruk");
        Assert.IsNotNull(gruk, "Gruk no está en el pueblo");
        var j = Ayudas.Jugador();
        GestorEscena.Colocar(j.transform, gruk.transform.position + Vector3.left * 1.1f, 90f);
        yield return null;
        Assert.AreEqual(gruk, Interactuable.MasCercano(j.transform.position));
        gruk.Usar(j);
        StringAssert.StartsWith("Gruk:", Dialogo.Ultimo);
        Dialogo.Cerrar();

        // Junto a la fogata, Maela.
        GestorEscena.Colocar(j.transform, carteles[0].transform.position + Vector3.left * 1.3f, 90f);   // al oeste: Tobin está al este
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(carteles[0], Interactuable.MasCercano(j.transform.position));
        carteles[0].Usar(j);
        yield return new WaitForSeconds(1.5f);
        Ayudas.Foto("80-maela-tutorial");
        Assert.AreEqual("Maela", Dialogo.Hablante);
        Assert.AreEqual(carteles[0].Frase, Cartel.Visible);

        Transicion.Ir("Pueblo");
        yield return Ayudas.EsperarEscena("Pueblo");
        Assert.AreEqual("Vado Gris", Transicion.Lugar);
    }
}
