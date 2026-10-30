using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Fase 12: el saqueo de punta a punta contra la API local (docker compose en api/, en modo de pruebas).
/// Un defensor construye un cofre y le mete 20 monedas; un atacante prepara el saqueo, entra, anda, mira,
/// marca el cofre y el cofre central y se lleva el botín; el defensor ve el informe al entrar en su fuerte.
public class PruebaSaqueo
{
    const string UrlLocal = "http://127.0.0.1:3100";

    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); Sesion.Reiniciar(); PlayerPrefs.SetString("url_api", UrlLocal); }

    [TearDown]
    public void Recoger()
    {
        PantallaPrepararSaqueo.RivalPedido = "";
        Saqueo.Datos = null;
        if (PantallaInforme.Abierta) Object.Destroy(PantallaInforme.Abierta.gameObject);
        PlayerPrefs.DeleteKey("url_api");
        Sesion.Reiniciar();
        EntradaJuego.MoverPantalla = Vector2.zero;
        Guardado.Activo = true;
    }

    static IEnumerator Esperar(System.Func<bool> condicion, float limite = 8f)
    {
        float t = 0f;
        while (!condicion() && t < limite) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static IEnumerator Llamar(string metodo, string ruta, object cuerpo)
    {
        bool listo = false;
        ApiCliente.Enviar(metodo, ruta, cuerpo, r =>
        {
            Assert.IsTrue(r.Ok, $"{metodo} {ruta}: {r.cuerpo}");
            if (r.cuerpo.Contains("\"jugador\"")) Sincronizacion.AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador);
            listo = true;
        });
        yield return Esperar(() => listo);
    }

    /// Cuenta invitada nueva con personaje. Devuelve (por "fin") el token del dispositivo, para volver a ella.
    static IEnumerator NuevaCuenta(string nombre, System.Action<string> fin)
    {
        Sesion.Reiniciar();
        Partida.Reemplazar(new DatosPartida());
        string dispositivo = System.Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString("token_dispositivo", dispositivo);
        bool? ok = null;
        Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true, "Sin sesión con la API");
        yield return Llamar("PUT", "/jugador/personaje", new MensajesApi.PeticionPersonaje { nombre = nombre, modelo = "character-male-a" });
        fin(dispositivo);
    }

    /// Anda con el joystick hasta el punto (como un jugador: la API rehace el camino con su velocidad).
    static IEnumerator IrA(Vector3 destino, float limite = 8f)
    {
        var j = Ayudas.Jugador();
        float t = 0f;
        while (t < limite)
        {
            Vector3 d = destino - j.transform.position; d.y = 0f;
            if (d.magnitude < 0.25f) break;
            EntradaJuego.MoverPantalla = new Vector2(d.x, d.z).normalized;
            t += Time.deltaTime;
            yield return null;
        }
        EntradaJuego.MoverPantalla = Vector2.zero;
        yield return null;
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator SaqueoDePuntaAPuntaContraLaApi()
    {
        using (var req = UnityWebRequest.Get(UrlLocal + "/salud"))
        {
            req.timeout = 2;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) Assert.Ignore("API no disponible en " + UrlLocal);
        }
        string sufijo = Random.Range(1000, 9999).ToString();
        string defensor = "Def" + sufijo, atacante = "Ata" + sufijo;

        // ---------- defensor: un cofre con 20 monedas ----------
        string dispDefensor = null;
        yield return NuevaCuenta(defensor, d => dispDefensor = d);
        yield return Llamar("POST", "/pueblo/botin", new MensajesApi.PeticionPueblo { recursos = new MensajesApi.Recursos { madera = 10 } });
        yield return Llamar("POST", "/mazmorra/resultado", new MensajesApi.PeticionMazmorra { jefe_muerto = false, recursos = new MensajesApi.Recursos { moneda = 30, hierro = 2 } });
        Viaje.llegarA = "";
        Transicion.Ir("Fuerte");
        yield return Ayudas.EsperarEscena("Fuerte");
        yield return new WaitForSeconds(1f);
        var ed = Object.FindAnyObjectByType<EditorFuerte>();
        ed.Entrar();
        ed.Seleccionar("cofre");
        Assert.IsTrue(ed.TocarCasilla(3, 3), ed.UltimoMotivo);
        ed.Salir();
        yield return Esperar(() => Partida.Actual.capacidad == 80, 6f);
        Assert.AreEqual(80, Partida.Actual.capacidad, "La API no guardó el cofre (capacidad 50 + 30)");
        ed.Entrar();
        ed.Seleccionar(EditorFuerte.Repartir);
        Assert.IsTrue(ed.TocarCasilla(3, 3), "Repartir no abre el cofre");
        var rep = PantallaReparto.Abierta;
        Assert.IsNotNull(rep);
        for (int i = 0; i < 4; i++) Assert.AreEqual(5, rep.Mover(Recurso.Moneda, 5));
        yield return null;
        Ayudas.Foto("70-reparto");
        rep.Guardar();
        yield return Esperar(() => !PantallaReparto.Abierta);
        Assert.AreEqual(20, Partida.Actual.fuerte.En(3, 3).contenido.moneda, "El reparto no llegó a la API: " + Avisos.Ultimo);
        ed.Salir();

        // ---------- atacante: preparar, entrar, mirar, marcar y resolver ----------
        yield return NuevaCuenta(atacante, _ => { });
        yield return Llamar("POST", "/mazmorra/resultado", new MensajesApi.PeticionMazmorra { jefe_muerto = false, recursos = new MensajesApi.Recursos { moneda = 10 } });
        yield return Ayudas.Cargar("Pueblo");
        PantallaPrepararSaqueo.RivalPedido = defensor;
        Ayudas.Puerta("asalto").Usar(Ayudas.Jugador());
        yield return Esperar(() => PantallaPrepararSaqueo.Abierta && PantallaPrepararSaqueo.Abierta.Preparado != null);
        var prep = PantallaPrepararSaqueo.Abierta;
        Assert.IsNotNull(prep?.Preparado, "No llegó el rival: " + prep?.Mensaje);
        Assert.AreEqual(defensor, prep.Preparado.rival.nombre);
        Assert.AreEqual(2, prep.Preparado.rival.escondites, "Cofre central + cofre a la vista");
        Ayudas.Foto("71-preparar-saqueo");
        prep.Saquear();
        yield return Ayudas.EsperarEscena("Asalto", 10f);
        yield return Esperar(() => Saqueo.Actual && Saqueo.Actual.Estado == Saqueo.Fase.Explorar);
        var s = Saqueo.Actual;
        Assert.AreEqual(Saqueo.Fase.Explorar, s.Estado, "No empezó la exploración: " + s.Error);
        Assert.AreEqual(5, Partida.Inventario.monedas, "No se cobró la tasa");

        yield return IrA(s.Fuerte.Mundo(3, 4));
        s.Mirar(3, 3);
        yield return Esperar(() => s.UltimaMirada.Length > 0, 6f);
        Assert.AreEqual("Pesa.", s.UltimaMirada);
        Ayudas.Foto("72-saqueo-mirar");
        yield return IrA(s.Fuerte.Mundo(7, 8));

        s.EmpezarMarcar();
        yield return null;
        Assert.IsTrue(s.AlternarMarca(3, 3));
        Assert.IsTrue(s.AlternarMarca(8, 7));
        Assert.IsFalse(s.AlternarMarca(0, 0), "Fuera del fuerte no se marca");
        yield return null;
        Ayudas.Foto("73-saqueo-marcar");
        s.Confirmar();
        yield return Esperar(() => s.Estado == Saqueo.Fase.Terminado, 15f);
        Assert.IsNotNull(s.Resultado, "Sin resultado: " + s.Error);
        Assert.AreEqual("botin", s.Resultado.marcas[0].resultado, "El cofre con 20 monedas no dio botín");
        Assert.AreEqual(10, s.Resultado.marcas[0].botin.moneda);
        Assert.AreEqual(0, s.Resultado.descartadas, "El servidor descartó entradas de un recorrido normal");
        Assert.Greater(s.Resultado.puntos_despues, s.Resultado.puntos_antes);
        yield return new WaitForSeconds(0.3f);
        Ayudas.Foto("74-saqueo-resultado");
        StringAssert.Contains("Botín", Object.FindAnyObjectByType<PantallaSaqueo>().TextoResultado);
        s.Volver();
        yield return Ayudas.EsperarEscena("Pueblo");

        // ---------- defensor: el informe al entrar en su fuerte ----------
        Sesion.Reiniciar();
        PlayerPrefs.SetString("token_dispositivo", dispDefensor);
        bool? ok = null;
        Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true);
        bool refrescado = false;
        Sincronizacion.Refrescar(_ => refrescado = true);
        yield return Esperar(() => refrescado);
        Viaje.llegarA = "";
        Transicion.Ir("Fuerte");
        yield return Ayudas.EsperarEscena("Fuerte");
        yield return Esperar(() => PantallaInforme.Abierta, 6f);
        Assert.IsNotNull(PantallaInforme.Abierta, "No sale el informe del saqueo al entrar en el fuerte");
        StringAssert.Contains(atacante, PantallaInforme.Abierta.Texto);
        yield return null;
        Ayudas.Foto("75-informe-saqueo");
        PantallaInforme.Abierta.Cerrar();
        Assert.AreEqual(10, Partida.Actual.fuerte.En(3, 3).contenido.moneda, "Al defensor le quedan 10 en el cofre");
    }
}
