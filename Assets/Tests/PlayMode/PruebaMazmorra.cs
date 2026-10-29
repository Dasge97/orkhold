using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Fase 3: enemigos (orco, arquero, esqueleto, jefe), trampas y botín del jefe.
public class PruebaMazmorra
{
    [SetUp]
    public void Limpiar() { Partida.Reemplazar(new DatosPartida()); }

    static T Cercano<T>(Vector3 a) where T : Component =>
        Object.FindObjectsByType<T>().OrderBy(x => Vector3.Distance(x.transform.position, a)).FirstOrDefault();

    [UnityTest]
    public IEnumerator MatarAlJefeDaLaUnidadYVuelveAlPueblo()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        var j = Ayudas.Jugador();
        j.Vida.invulnerable = true;   // modo prueba: vida infinita
        var jefe = Object.FindAnyObjectByType<Jefe>();
        Assert.IsNotNull(jefe, "La mazmorra no tiene jefe");
        Assert.AreEqual(15, jefe.Vida.maxima);
        Assert.AreEqual(2, jefe.dano);

        GestorEscena.Colocar(j.transform, jefe.transform.position + Vector3.back * 1.1f, 0f);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(BarraVidaJefe.Visible, "No aparece la barra de vida del jefe");
        Ayudas.Foto("43-jefe");

        int ataques = 0;
        while (!jefe.Vida.Muerto && ataques < 60)
        {
            // Pegado al jefe aunque se mueva o empuje.
            if (Ayudas.DistanciaPlana(j.transform.position, jefe.transform.position) > 1.3f)
                GestorEscena.Colocar(j.transform, jefe.transform.position + (j.transform.position - jefe.transform.position).normalized * 1f, 0f);
            EntradaJuego.AtaquePantalla = true;
            ataques++;
            yield return new WaitForSeconds(0.7f);
        }
        Assert.IsTrue(jefe.Vida.Muerto, $"El jefe sigue vivo tras {ataques} ataques");
        Assert.AreEqual(1, Partida.Actual.unidades.Count(u => u.tipo == Jefe.TipoUnidad), "No hay Orco jefe defensor en el inventario");
        Assert.AreEqual("1", Partida.Actual.Progreso(Jefe.ClaveProgreso));
        yield return Ayudas.EsperarEscena("Pueblo", 12f);
        Assert.Less(Ayudas.DistanciaPlana(Ayudas.Jugador().transform.position, Ayudas.Puerta("mazmorra1").llegada), 0.3f);
    }

    [UnityTest]
    public IEnumerator LaSegundaVezElJefeDaTresHuesos()
    {
        Partida.Actual.PonerProgreso(Jefe.ClaveProgreso, "1");
        yield return Ayudas.Cargar("Mazmorra1");
        var jefe = Object.FindAnyObjectByType<Jefe>();
        jefe.Vida.Danar(99);
        yield return null;
        Assert.AreEqual(0, Partida.Actual.unidades.Count, "La segunda vez no da unidad");
        Assert.AreEqual(3, Partida.Inventario.hueso);
    }

    [UnityTest]
    public IEnumerator ElJefeAvisaYGolpeaEnArea()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        var j = Ayudas.Jugador();
        var jefe = Object.FindAnyObjectByType<Jefe>();
        jefe.intervaloArea = 0.5f;
        GestorEscena.Colocar(j.transform, jefe.transform.position + Vector3.back * 1.6f, 0f);
        float t = 0f;
        while (jefe.GolpesArea == 0 && t < 3f) { t += Time.deltaTime; yield return null; }
        Assert.Greater(jefe.GolpesArea, 0, "El jefe no lanza el golpe en área");
        Assert.IsNotNull(Object.FindAnyObjectByType<AvisoArea>(), "No aparece el círculo rojo de aviso");
        yield return new WaitForSeconds(0.3f);
        Ayudas.Foto("44-jefe-aviso-area");
        int antes = j.Vida.actual;
        yield return new WaitForSeconds(1f);
        Assert.Less(j.Vida.actual, antes, "El golpe en área no hace daño dentro del círculo");
    }

    [UnityTest]
    public IEnumerator ElOrcoPersigueYPega()
    {
        yield return Ayudas.Cargar("Pueblo");
        var j = Ayudas.Jugador();
        var orco = Object.FindObjectsByType<Enemigo>().First(e => e.GetType() == typeof(Enemigo));
        GestorEscena.Colocar(j.transform, orco.transform.position + Vector3.right * 3f, 0f);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Enemigo.Estado.Persigue, orco.EstadoActual, "A 3 m el orco debería perseguir");
        yield return new WaitForSeconds(3f);
        Assert.Less(j.Vida.actual, j.Vida.maxima, "El orco no pega");

        // Lejos: deja de perseguir y vuelve a su sitio.
        GestorEscena.Colocar(j.transform, orco.Casa + Vector3.right * 15f, 0f);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Enemigo.Estado.Vuelve, orco.EstadoActual);
    }

    [UnityTest]
    public IEnumerator LosOrcosDelPuebloPatrullan()
    {
        yield return Ayudas.Cargar("Pueblo");
        var orco = Object.FindObjectsByType<Enemigo>().First(e => e.patrulla);
        var j = Ayudas.Jugador();
        GestorEscena.Colocar(j.transform, orco.Casa + Vector3.right * 20f + Vector3.forward * 20f, 0f);
        Vector3 inicio = orco.transform.position;
        yield return new WaitForSeconds(2.5f);
        Assert.Greater(Ayudas.DistanciaPlana(orco.transform.position, inicio), 0.8f, "El orco no se mueve al patrullar");
    }

    [UnityTest]
    public IEnumerator ElOrcoMuertoSueltaMonedasYReaparece()
    {
        yield return Ayudas.Cargar("Pueblo");
        var orco = Object.FindObjectsByType<Enemigo>().First(e => e.GetType() == typeof(Enemigo));
        orco.reaparecerSegundos = 3f;
        int recogiblesAntes = Object.FindObjectsByType<Recogible>().Length;
        orco.RecibirGolpe(99, orco.transform.position + Vector3.forward, Ayudas.Jugador());
        yield return null;
        Assert.AreEqual(Enemigo.Estado.Muerto, orco.EstadoActual);
        Assert.Greater(Object.FindObjectsByType<Recogible>().Length, recogiblesAntes, "No suelta monedas");
        Assert.AreEqual(1, Partida.Actual.bajas);
        yield return new WaitForSeconds(3.5f);
        Assert.AreNotEqual(Enemigo.Estado.Muerto, orco.EstadoActual, "No reaparece");
        Assert.AreEqual(orco.Vida.maxima, orco.Vida.actual);
        Assert.Less(Ayudas.DistanciaPlana(orco.transform.position, orco.Casa), 1.2f, "No reaparece en su sitio (patrulla nada más volver)");
    }

    [UnityTest]
    public IEnumerator ElArqueroDisparaDesdeLejosYHuyeSiTeAcercas()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        var j = Ayudas.Jugador();
        j.Vida.invulnerable = true;
        var arquero = Object.FindAnyObjectByType<EnemigoArquero>();
        Assert.IsNotNull(arquero, "La mazmorra no tiene arqueros");
        Assert.AreEqual(2, arquero.Vida.maxima);
        GestorEscena.Colocar(j.transform, arquero.transform.position + Vector3.back * 4f, 0f);
        float t = 0f;
        while (arquero.Disparos == 0 && t < 3f) { t += Time.deltaTime; yield return null; }
        Assert.Greater(arquero.Disparos, 0, "El arquero no dispara a 4 m");
        yield return new WaitForSeconds(0.25f);
        Ayudas.Foto("45-arquero-dispara");

        GestorEscena.Colocar(j.transform, arquero.transform.position + Vector3.back * 1.3f, 0f);
        float antes = Ayudas.DistanciaPlana(j.transform.position, arquero.transform.position);
        yield return new WaitForSeconds(0.6f);
        Assert.Greater(Ayudas.DistanciaPlana(j.transform.position, arquero.transform.position), antes + 0.3f, "El arquero no huye");
    }

    [UnityTest]
    public IEnumerator ElEsqueletoEsLentoYFuerte()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        var esq = Object.FindObjectsByType<Enemigo>().FirstOrDefault(e => e.nombre == "Esqueleto");
        Assert.IsNotNull(esq, "La mazmorra no tiene esqueletos");
        Assert.AreEqual(4, esq.Vida.maxima);
        Assert.AreEqual(2, esq.dano);
        Assert.AreEqual(1.5f, esq.velocidad);
    }

    [UnityTest]
    public IEnumerator LasTrampasPinchanCadaDosSegundos()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        var j = Ayudas.Jugador();
        var trampa = Object.FindAnyObjectByType<Trampa>();
        Assert.IsNotNull(trampa, "La mazmorra no tiene trampas");
        foreach (var e in Enemigo.Todos.ToArray()) e.gameObject.SetActive(false);
        GestorEscena.Colocar(j.transform, trampa.transform.position, 0f);
        int antes = j.Vida.actual;
        yield return new WaitForSeconds(2.1f);
        int tras1 = j.Vida.actual;
        Assert.AreEqual(antes - 1, tras1, "En 2 s la trampa debería quitar 1 de vida");
        GestorEscena.Colocar(j.transform, trampa.transform.position, 0f);
        yield return new WaitForSeconds(2.0f);
        Assert.AreEqual(tras1 - 1, j.Vida.actual, "La trampa no vuelve a pinchar a los 2 s");
    }
}
