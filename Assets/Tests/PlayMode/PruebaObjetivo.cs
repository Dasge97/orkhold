using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// Objetivo de ataque (01-11-2026): con un árbol y un orco al lado, el aro rojo marca al orco y el golpe va al orco.
public class PruebaObjetivo
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; }

    [UnityTest]
    public IEnumerator ElOrcoGanaAlArbol()
    {
        yield return Ayudas.Cargar("Pueblo");
        var j = Ayudas.Jugador();
        var arbol = Object.FindObjectsByType<FuenteRecurso>().First(f => f.recurso == Recurso.Madera);
        var orco = Object.FindObjectsByType<Enemigo>().First(e => e.GetType() == typeof(Enemigo));
        // Jugador a 0,8 m del árbol, mirando al árbol; el orco a 1,2 m, a su espalda.
        Vector3 haciaArbol = Vector3.forward;
        GestorEscena.Colocar(j.transform, arbol.transform.position - haciaArbol * 0.8f, 0f);
        orco.enabled = false;   // quieto, para que no se mueva durante la prueba
        orco.transform.position = j.transform.position - haciaArbol * 1.2f;
        yield return null;
        yield return null;
        Assert.AreSame(orco, j.Objetivo as Enemigo, "El objetivo debería ser el orco, no el árbol");
        Assert.AreEqual(orco.transform, AroInteraccion.ObjetivoMarcado, "El aro rojo no marca al orco");
        int vida = orco.Vida.actual;
        EntradaJuego.AtaquePantalla = true;
        yield return new WaitForSeconds(0.8f);
        Assert.Less(orco.Vida.actual, vida, "El golpe no fue al orco");
    }
}
