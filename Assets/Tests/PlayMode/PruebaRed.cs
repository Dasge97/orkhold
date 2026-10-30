using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 7: pueblo con red (modo anfitrión en los tests) y prefabs de red bien hechos.
public class PruebaRed
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; }

    [Test]
    public void LosPrefabsDeRedTienenIdentificadoresDistintos()
    {
        var hashes = PrefabsRed.Instancia.Todos().Select(p => p.GetComponent<NetworkObject>().PrefabIdHash).ToList();
        Assert.AreEqual(9, hashes.Count);
        Assert.IsFalse(hashes.Contains(0u), "Hay prefabs de red sin identificador");
        Assert.AreEqual(hashes.Count, hashes.Distinct().Count(), "Hay prefabs de red con el mismo identificador");
    }

    [UnityTest]
    public IEnumerator ElPuebloArrancaComoAnfitrionYLaRedCreaJugadorEnemigosYMonedas()
    {
        yield return Ayudas.Cargar("Pueblo");
        var red = RedPueblo.Actual;
        Assert.IsNotNull(red, "El pueblo no tiene RedPueblo");
        Assert.AreEqual(RedPueblo.Modo.Anfitrion, red.ModoActual);
        Assert.IsTrue(red.Red.IsHost);
        var j = ControlJugador.Local;
        Assert.IsNotNull(j, "No hay jugador local");
        Assert.IsNotNull(j.GetComponent<JugadorRed>(), "El jugador del pueblo no es de red");
        Assert.IsTrue(j.TieneAutoridad);
        Assert.AreEqual(8, Object.FindObjectsByType<EnemigoRed>().Length, "Los 8 orcos del pueblo deberían ser de red");
        Assert.Greater(Object.FindObjectsByType<RecogibleRed>().Length, 0, "Las monedas del pueblo deberían ser de red");
        Assert.AreEqual(0, Object.FindObjectsByType<Enemigo>().Count(e => !e.GetComponent<EnemigoRed>()), "Quedan orcos horneados");
        Assert.Less(Ayudas.DistanciaPlana(j.transform.position, GestorEscena.Actual.aparicion), 1f, "No aparece en la fogata");
    }

    [UnityTest]
    public IEnumerator AlSalirPorUnaPuertaSeCierraLaRedYAlVolverSeAbre()
    {
        yield return Ayudas.Cargar("Pueblo");
        yield return Ayudas.Pisar(Ayudas.Puerta("mazmorra1"));
        yield return Ayudas.EsperarEscena("Mazmorra1");
        Assert.IsNull(NetworkManager.Singleton, "La red sigue abierta fuera del pueblo");
        yield return Ayudas.Pisar(Ayudas.Puerta("salida"));
        yield return Ayudas.EsperarEscena("Pueblo");
        yield return null;
        Assert.IsNotNull(NetworkManager.Singleton);
        Assert.IsTrue(NetworkManager.Singleton.IsHost);
        Assert.Less(Ayudas.DistanciaPlana(ControlJugador.Local.transform.position, Ayudas.Puerta("mazmorra1").llegada), 1f,
            "Al volver no aparece junto a la puerta de la mazmorra");
    }
}
