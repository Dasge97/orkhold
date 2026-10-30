using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 10.4: el balance de la API cambia los costes del catálogo y los tiempos de reaparición del pueblo.
public class PruebaBalance
{
    const string Json = "{\"version\":7,\"lista\":[" +
        "{\"clave\":\"pieza.muro_madera.coste.madera\",\"valor\":5}," +
        "{\"clave\":\"pieza.muro_madera.vida\",\"valor\":8}," +
        "{\"clave\":\"fuerte.limite_trampa\",\"valor\":2}," +
        "{\"clave\":\"mundo.enemigos_reaparecer\",\"valor\":45}," +
        "{\"clave\":\"mundo.recursos_reaparecer\",\"valor\":200}]}";

    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Balance.Olvidar(); Guardado.Activo = true; }

    [UnityTest]
    public IEnumerator ElBalanceCambiaCostesVidasLimitesYTiempos()
    {
        Balance.Recibir(Json);
        Assert.AreEqual(7, Balance.Version);
        var cat = CatalogoPiezas.Instancia;
        var muro = cat.Buscar("muro_madera");
        Assert.AreEqual(1, muro.costes.Count);
        Assert.AreEqual(5, muro.costes[0].cantidad, "El coste del muro no sale del balance");
        Assert.AreEqual(8, muro.vida);
        Assert.AreEqual(2, cat.maxTrampas);
        // Una clave que el balance no trae deja el valor del catálogo.
        Assert.AreEqual(3, cat.Buscar("muro_piedra").costes[0].cantidad);

        // Con 4 de madera ya no llega para un muro.
        yield return Ayudas.Cargar("Fuerte");
        Partida.Inventario.madera = 4;
        var mapa = Fuerte.Actual.Mapa;
        Assert.IsFalse(ReglasFuerte.PuedePoner(Partida.Actual.fuerte, cat, muro, mapa, Partida.Actual, 2, 2, out var motivo));
        StringAssert.Contains("madera", motivo);
        Partida.Inventario.madera = 5;
        Assert.IsTrue(ReglasFuerte.PuedePoner(Partida.Actual.fuerte, cat, muro, mapa, Partida.Actual, 2, 2, out _));

        // En el pueblo, enemigos y árboles usan los tiempos del balance.
        yield return Ayudas.Cargar("Pueblo");
        yield return null;
        var e = Object.FindAnyObjectByType<Enemigo>();
        var f = Object.FindAnyObjectByType<FuenteRecurso>();
        Assert.IsNotNull(e); Assert.IsNotNull(f);
        Assert.AreEqual(45f, e.reaparecerSegundos);
        Assert.AreEqual(200f, f.segundosAgotado);
    }
}
