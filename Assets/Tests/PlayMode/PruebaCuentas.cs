using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// Fase 6: arranque y personaje, tablón, y el juego contra la API de verdad (docker compose en api/).
public class PruebaCuentas
{
    const string UrlLocal = "http://127.0.0.1:3100";

    [SetUp]
    public void Preparar()
    {
        Guardado.Activo = false;
        Partida.Reemplazar(new DatosPartida());
        Sesion.Cerrar();
        PlayerPrefs.DeleteKey("url_api");
    }

    [TearDown]
    public void Recoger()
    {
        PlayerPrefs.DeleteKey("url_api");
        PlayerPrefs.DeleteKey("token_dispositivo");
        Sesion.Cerrar();
        Guardado.Activo = true;
    }

    [UnityTest]
    public IEnumerator ElArranqueCreaElPersonajeYEntraAlPueblo()
    {
        SceneManager.LoadScene("Arranque");
        yield return null; yield return null;
        var p = Object.FindAnyObjectByType<PantallaArranque>();
        Assert.IsNotNull(p, "La escena Arranque no tiene PantallaArranque");
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(PantallaArranque.Fase.CrearPersonaje, p.Estado, "Sin nombre debería pedir crear el personaje");
        p.Elegir(1);
        yield return null;
        Ayudas.Foto("50-arranque-personaje");
        p.Empezar("ab");
        Assert.AreEqual(PantallaArranque.Fase.CrearPersonaje, p.Estado, "Un nombre de 2 letras no vale");
        p.Empezar("Probador");
        Assert.AreEqual("Probador", Partida.Actual.nombre);
        Assert.AreEqual(Catalogo.Instancia.idsPersonajes[1], Partida.Actual.modelo);
        yield return Ayudas.EsperarEscena("Pueblo");
        var pj = Ayudas.Jugador().GetComponent<PersonajeJugador>();
        Assert.AreEqual(Partida.Actual.modelo, pj.Actual, "El jugador no lleva el modelo elegido");
        Assert.IsNotNull(Ayudas.Jugador().animador, "El modelo nuevo no tiene animador");
        yield return new WaitForSeconds(0.3f);
        Ayudas.Foto("51-pueblo-con-personaje");
    }

    [UnityTest]
    public IEnumerator ElTablonAbreElRanking()
    {
        yield return Ayudas.Cargar("Pueblo");
        var t = Object.FindAnyObjectByType<Tablon>();
        Assert.IsNotNull(t, "El pueblo no tiene tablón");
        t.Usar(Ayudas.Jugador());
        yield return null;
        Assert.IsTrue(PantallaRanking.Abierta);
        StringAssert.Contains("1000", PantallaRanking.TextoRanking);
        PantallaRanking.Cerrar();
    }

    // ---------- contra la API (si está levantada con docker compose) ----------

    static IEnumerator HayApi(System.Action<bool> fin)
    {
        using var req = UnityWebRequest.Get(UrlLocal + "/salud");
        req.timeout = 2;
        yield return req.SendWebRequest();
        fin(req.result == UnityWebRequest.Result.Success);
    }

    static IEnumerator Esperar(System.Func<bool> condicion, float limite = 8f)
    {
        float t = 0f;
        while (!condicion() && t < limite) { t += Time.unscaledDeltaTime; yield return null; }
    }

    static IEnumerator NuevaCuenta(string nombre)
    {
        Sesion.Cerrar();
        Partida.Reemplazar(new DatosPartida());
        PlayerPrefs.SetString("token_dispositivo", System.Guid.NewGuid().ToString("N"));
        bool? ok = null;
        Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true, "No hay sesión con la API");
        bool listo = false;
        ApiCliente.Put("/jugador/personaje", new MensajesApi.PeticionPersonaje { nombre = nombre, modelo = "character-male-a" }, r =>
        {
            Assert.IsTrue(r.Ok, r.cuerpo);
            Sincronizacion.AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador);
            listo = true;
        });
        yield return Esperar(() => listo);
    }

    [UnityTest, Timeout(120000)]
    public IEnumerator JugarContraLaApi()
    {
        bool hay = false;
        yield return HayApi(x => hay = x);
        if (!hay) Assert.Ignore("API no disponible en " + UrlLocal + " (docker compose up en api/)");
        PlayerPrefs.SetString("url_api", UrlLocal);
        string sufijo = Random.Range(1000, 9999).ToString();

        // Jugador 1: recoge madera en el pueblo, gana botín en la mazmorra y construye 5 muros.
        yield return NuevaCuenta("Def" + sufijo);
        string id1 = Sesion.JugadorId;
        yield return Ayudas.Cargar("Pueblo");
        Partida.Sumar(Recurso.Madera, 10);
        Sincronizacion.AnotarPueblo(Recurso.Madera, 10);
        yield return Esperar(() => Sincronizacion.PendientePueblo.madera == 0, 12f);
        Assert.AreEqual(0, Sincronizacion.PendientePueblo.madera, "El botín del pueblo no llegó a la API");

        Viaje.llegarA = "";
        Transicion.Ir("Mazmorra1");
        yield return Ayudas.EsperarEscena("Mazmorra1");
        Partida.Sumar(Recurso.Moneda, 7);
        Transicion.Ir("Pueblo");
        yield return Ayudas.EsperarEscena("Pueblo");
        bool refrescado = false;
        yield return new WaitForSeconds(1f);
        Sincronizacion.Refrescar(_ => refrescado = true);
        yield return Esperar(() => refrescado);
        Assert.AreEqual(7, Partida.Inventario.monedas, "El botín de la mazmorra no llegó a la API");
        Assert.AreEqual(10, Partida.Inventario.madera);

        Viaje.llegarA = "";
        Transicion.Ir("Fuerte");
        yield return Ayudas.EsperarEscena("Fuerte");
        var ed = Object.FindAnyObjectByType<EditorFuerte>();
        ed.Entrar();
        ed.Seleccionar("muro_madera");
        for (int x = 3; x <= 7; x++) Assert.IsTrue(ed.TocarCasilla(x, 3), ed.UltimoMotivo);
        ed.Salir();
        refrescado = false;
        yield return new WaitForSeconds(1.5f);
        Sincronizacion.Refrescar(_ => refrescado = true);
        yield return Esperar(() => refrescado);
        Assert.AreEqual(5, Partida.Actual.fuerte.piezas.Count, "La API no guardó el fuerte");
        Assert.AreEqual(0, Partida.Inventario.madera, "La API no descontó la madera");

        // Jugador 2: busca rival en la API, asalta, gana y sube en el ranking.
        yield return NuevaCuenta("Ata" + sufijo);
        yield return Ayudas.Cargar("Pueblo");
        var cofre = Ayudas.Puerta("asalto");
        cofre.Usar(Ayudas.Jugador());
        yield return Ayudas.EsperarEscena("Asalto", 10f);
        Assert.IsNotNull(Fuerte.DisenoAsalto);
        StringAssert.Contains("(", BuscarRival.Rival, "El rival no viene de la API");
        int antes = Partida.Actual.puntuacion;
        yield return new WaitForSeconds(5.5f);
        GestorEscena.Colocar(Ayudas.Jugador().transform, Asalto.Actual.PosicionCofre + Vector3.left * 0.8f, 90f);
        Asalto.Resultado res = null;
        System.Action<Asalto.Resultado> f = r => res = r;
        Asalto.AlTerminar += f;
        yield return Esperar(() => res != null, 10f);
        Asalto.AlTerminar -= f;
        Assert.IsNotNull(res, "No llegó el resultado del asalto");
        Assert.IsTrue(res.ganado);
        Assert.Greater(res.puntosDespues, antes, "La API no subió la puntuación");
        Assert.AreEqual(res.puntosDespues, Partida.Actual.puntuacion);
        Assert.GreaterOrEqual(Partida.Inventario.monedas, 10, "La API no dio la recompensa");

        // El ranking y el historial del atacante lo reflejan.
        bool hecho = false; MensajesApi.RespuestaHistorial h = null;
        ApiCliente.Get("/asaltos/historial", r => { h = r.Leer<MensajesApi.RespuestaHistorial>(); hecho = true; });
        yield return Esperar(() => hecho);
        Assert.AreEqual(1, h.hechos.Count);
        Assert.IsTrue(h.hechos[0].ganado);
        Assert.IsNotEmpty(id1);
    }

    /// En git la configuración va sin URL (modo local); la URL solo se pone al compilar el APK.
    [Test]
    public void LaConfiguracionDelRepositorioEsModoLocal()
    {
        Assert.IsEmpty(Configuracion.Instancia.urlApi, "Resources/Configuracion.asset tiene una URL de API: debería ir vacía en git");
        Assert.IsEmpty(Configuracion.Instancia.hostMundo, "Resources/Configuracion.asset tiene un servidor de mundo: debería ir vacío en git");
    }

    [System.Serializable] class ListaBalance { public System.Collections.Generic.List<Balance.Par> lista; }

    /// La API tiene su copia de catálogo (los números en balance.json), mapa del fuerte y fuertes del sistema:
    /// sin balance descargado, el cliente tiene que usar los mismos valores por defecto.
    [Test]
    public void ElServidorUsaLasMismasReglasQueElCliente()
    {
        string Leer(string ruta) => System.IO.File.ReadAllText(ruta).Replace("\r", "");
        Balance.Olvidar();
        var valores = JsonUtility.FromJson<ListaBalance>("{\"lista\":" + Leer("api/src/datos/balance.json") + "}").lista
            .ToDictionary(p => p.clave, p => p.valor);
        var catalogo = Leer("api/src/datos/catalogo.json");
        var cat = CatalogoPiezas.Instancia;
        foreach (var p in cat.piezas)
        {
            StringAssert.Contains($"\"id\": \"{p.id}\"", catalogo, "La API no tiene la pieza " + p.id);
            string prefijo = (p.categoria == CatalogoPiezas.Categoria.Escondite ? "escondite." : "pieza.") + p.id;
            if (p.categoria != CatalogoPiezas.Categoria.Escondite)
                Assert.AreEqual(p.vida, (int)valores[prefijo + ".vida"], p.id + ": vida distinta");
            foreach (var c in p.costes)
                Assert.AreEqual(c.cantidad, (int)valores[$"{prefijo}.coste.{Nombres.Clave(c.recurso)}"], $"{p.id}: coste de {c.recurso} distinto");
            foreach (var r in new[] { Recurso.Moneda, Recurso.Madera, Recurso.Piedra, Recurso.Hierro, Recurso.Hueso })
                if (valores.ContainsKey($"{prefijo}.coste.{Nombres.Clave(r)}") && string.IsNullOrEmpty(p.requiereUnidad))
                    Assert.IsTrue(p.costes.Any(c => c.recurso == r), $"{p.id}: al cliente le falta el coste de {r}");
        }
        Assert.AreEqual(cat.maxMuros, (int)valores["fuerte.limite_muro"]);
        Assert.AreEqual(cat.maxTrampas, (int)valores["fuerte.limite_trampa"]);
        Assert.AreEqual(cat.maxDefensores, (int)valores["fuerte.limite_defensor"]);
        Assert.AreEqual(Leer("Assets/Mapas/fuerte-base.txt"), Leer("api/src/datos/fuerte-base.txt"), "fuerte-base.txt distinto en la API");
        foreach (var n in new[] { "facil", "medio", "dificil" })
            Assert.AreEqual(Leer($"Assets/Mapas/fuertes-prueba/{n}.json"), Leer($"api/src/datos/fuertes-sistema/{n}.json"), n + ".json distinto en la API");
    }
}
