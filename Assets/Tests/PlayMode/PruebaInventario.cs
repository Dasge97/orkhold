using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// Inventario (31-10-2026): el HUD ya no enseña los recursos; el botón del cofre abre una pantalla con cada uno.
public class PruebaInventario
{
    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); }

    [TearDown]
    public void Recoger() { Guardado.Activo = true; PantallaInventario.Cerrar(); }

    [UnityTest]
    public IEnumerator ElBotonDelCofreAbreElInventario()
    {
        yield return Ayudas.Cargar("Mazmorra1");
        Partida.Sumar(Recurso.Madera, 7);
        var boton = GameObject.Find("BotonInventario");
        Assert.IsNotNull(boton, "No hay botón de inventario en el HUD");
        Assert.IsNull(GameObject.Find("Recursos"), "El panel de recursos sigue en el HUD");
        boton.GetComponent<Button>().onClick.Invoke();
        yield return null;
        Assert.IsTrue(PantallaInventario.Abierta);
        Assert.AreEqual("7", PantallaInventario.Cantidad(Recurso.Madera));
        Partida.Sumar(Recurso.Moneda, 3);
        Assert.AreEqual("3", PantallaInventario.Cantidad(Recurso.Moneda), "No se actualiza abierto");
        Ayudas.Foto("90-inventario");
        PantallaInventario.Cerrar();
        Assert.IsFalse(PantallaInventario.Abierta);
    }
}
