using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Fase 14 (11-mazmorras.md §2): la puerta de la Forja Hundida se abre al vencer a Urzog; orco herrero con
/// escudo, bombardero, trampas de fuego y Grimbra con sus refuerzos; el muro de hierro se abre con Grimbra.
public class PruebaForja
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; }

    [UnityTest]
    public IEnumerator LaPuertaSeAbreAlVencerAUrzog()
    {
        yield return Ayudas.Cargar("Pueblo");
        var puerta = Ayudas.Puerta("mazmorra2");
        var pm = puerta.GetComponent<PuertaMazmorra>();
        Assert.IsNotNull(pm, "La puerta de la mazmorra 2 no comprueba el progreso");
        Assert.IsFalse(pm.Abierta);
        puerta.Ir();
        yield return null;
        StringAssert.Contains("Ansel", Avisos.Ultimo);
        Assert.AreEqual("Pueblo", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        // Las de la 3 y la 4 existen en el pueblo, pero todavía no llevan a ninguna parte.
        Assert.IsNotNull(Ayudas.Puerta("mazmorra3").GetComponent<PuertaMazmorra>());
        Assert.IsNotNull(Ayudas.Puerta("mazmorra4").GetComponent<PuertaMazmorra>());

        Partida.Actual.PonerProgreso("jefe1_muerto", "1");
        Assert.IsTrue(pm.Abierta);
        puerta.Ir();
        yield return Ayudas.EsperarEscena("Mazmorra2");
        Assert.AreEqual(2, GestorEscena.Actual.numeroMazmorra);
        Assert.AreEqual("La Forja Hundida", Transicion.Lugar);
    }

    [UnityTest]
    public IEnumerator EnemigosDeLaForjaYGrimbra()
    {
        yield return Ayudas.Cargar("Mazmorra2");
        yield return null;
        var j = Ayudas.Jugador();
        j.Vida.invulnerable = true;
        var herreros = Object.FindObjectsByType<OrcoHerrero>();
        Assert.Greater(herreros.Length, 3, "La Forja debería tener orcos herreros");
        Assert.Greater(Object.FindObjectsByType<Bombardero>().Length, 1);
        Assert.Greater(Object.FindObjectsByType<Trampa>().Count(t => t.periodo == 4f), 4, "Faltan trampas de fuego");

        // Orco herrero: vida 6 × 1,3 = 8; el primer golpe de cada tres lo para el escudo.
        var h = herreros[0];
        Assert.AreEqual(8, h.Vida.maxima);
        for (int i = 0; i < 3; i++) h.RecibirGolpe(1, h.transform.position + Vector3.left, j);
        Assert.AreEqual(1, h.Parados);
        Assert.AreEqual(6, h.Vida.actual);

        // Bombardero: con el jugador cerca, lanza bombas con aviso.
        var b = Object.FindObjectsByType<Bombardero>()[0];
        GestorEscena.Colocar(j.transform, b.transform.position + Vector3.forward * 4f, 180f);
        yield return new WaitForSeconds(2.5f);
        Assert.Greater(b.Bombas, 0, "El bombardero no ha lanzado ninguna bomba");
        Ayudas.Foto("90-forja-bombardero");

        // Grimbra: vida del balance (25 por defecto); a media vida llama a 2 herreros.
        var g = Object.FindObjectsByType<Jefe>().First(x => x.numeroMazmorra == 2);
        Assert.AreEqual("Grimbra la Fundidora", g.nombre);
        Assert.AreEqual(25, g.Vida.maxima);
        GestorEscena.Colocar(j.transform, g.transform.position + Vector3.back * 2.5f, 0f);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < 13; i++) g.RecibirGolpe(1, g.transform.position + Vector3.back, j);
        yield return null;
        Assert.AreEqual(2, g.Refuerzos, "Grimbra no llamó a los herreros a media vida");
        Ayudas.Foto("91-grimbra");
        for (int i = 0; i < 12; i++) g.RecibirGolpe(1, g.transform.position + Vector3.back, j);
        yield return null;
        Assert.IsTrue(g.Vida.Muerto);
        Assert.AreEqual("1", Partida.Actual.Progreso("jefe2_muerto"));
        StringAssert.Contains("Grimbra", Avisos.Ultimo);
    }

    [Test]
    public void ElMuroDeHierroSeAbreConGrimbra()
    {
        var cat = CatalogoPiezas.Instancia;
        var muro = cat.Buscar("muro_hierro");
        Assert.IsNotNull(muro, "Falta el muro de hierro en el catálogo");
        Assert.AreEqual(2, muro.mazmorra);
        Partida.Inventario.piedra = 10; Partida.Inventario.hierro = 10;
        var mapa = CargadorMapa.Leer(System.IO.File.ReadAllText("Assets/Mapas/fuerte-base.txt"));
        Assert.IsFalse(ReglasFuerte.PuedePoner(Partida.Actual.fuerte, cat, muro, mapa, Partida.Actual, 3, 3, out var motivo));
        StringAssert.Contains("mazmorra 2", motivo);
        Partida.Actual.PonerProgreso("jefe2_muerto", "1");
        Assert.IsTrue(ReglasFuerte.PuedePoner(Partida.Actual.fuerte, cat, muro, mapa, Partida.Actual, 3, 3, out motivo), motivo);
    }
}
