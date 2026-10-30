using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

/// Fase 11.3: cuentas con registro contra la API local (docker compose en api/): guardar el progreso de la
/// invitada, entrar desde "otro móvil", cerrar sesión (pide entrar) y borrar la cuenta.
public class PruebaRegistro
{
    const string UrlLocal = "http://127.0.0.1:3100";

    [SetUp]
    public void Preparar() { Guardado.Activo = false; Partida.Reemplazar(new DatosPartida()); Sesion.Reiniciar(); PlayerPrefs.SetString("url_api", UrlLocal); }

    [TearDown]
    public void Recoger()
    {
        if (PantallaCuenta.Abierta) Object.Destroy(PantallaCuenta.Abierta.gameObject);
        PlayerPrefs.DeleteKey("url_api");
        Sesion.Reiniciar();
        Guardado.Activo = true;
    }

    static IEnumerator Esperar(System.Func<bool> condicion, float limite = 8f)
    {
        float t = 0f;
        while (!condicion() && t < limite) { t += Time.unscaledDeltaTime; yield return null; }
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator GuardarProgresoEntrarEnOtroMovilSalirYBorrar()
    {
        using (var req = UnityWebRequest.Get(UrlLocal + "/salud"))
        {
            req.timeout = 2;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) Assert.Ignore("API no disponible en " + UrlLocal);
        }
        yield return Ayudas.Cargar("Pueblo");
        string correo = $"prueba{Random.Range(100000, 999999)}@ejemplo.com";
        const string clave = "clave-de-prueba-1";

        // Invitada con personaje.
        bool? ok = null;
        Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true, "Sin sesión de invitada");
        bool listo = false;
        ApiCliente.Put("/jugador/personaje", new MensajesApi.PeticionPersonaje { nombre = "Reg" + Random.Range(1000, 9999), modelo = "character-male-a" },
            r => { Sincronizacion.AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador); listo = true; });
        yield return Esperar(() => listo);
        string id = Sesion.JugadorId, nombre = Partida.Actual.nombre;

        // Guardar el progreso desde la ventana de la cuenta.
        var p = PantallaCuenta.Abrir(PantallaCuenta.Modo.Cuenta);
        yield return new WaitForSeconds(1f);
        Ayudas.Foto("60-cuenta-invitada");
        p.Registrar(correo, "corta");
        StringAssert.Contains("8 caracteres", p.Mensaje);
        p.Registrar(correo, clave);
        yield return Esperar(() => !PantallaCuenta.Abierta);
        Assert.IsTrue(Sesion.Registrada, "La cuenta no quedó registrada");
        Assert.AreEqual(id, Sesion.JugadorId, "Registrar cambió de jugador");

        // "Otro móvil": sin nada guardado entra como invitada nueva; con la ventana de entrar, vuelve a su cuenta.
        Sesion.Reiniciar();
        Partida.Reemplazar(new DatosPartida());
        ok = null; Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.AreNotEqual(id, Sesion.JugadorId, "Un móvil nuevo debería empezar con otra invitada");
        p = PantallaCuenta.Abrir(PantallaCuenta.Modo.Entrar);
        yield return null;
        Ayudas.Foto("61-entrar-con-cuenta");
        p.Entrar(correo, "mala-clave");
        yield return Esperar(() => p.Mensaje.Contains("incorrectos"));
        StringAssert.Contains("incorrectos", p.Mensaje);
        p.Entrar(correo, clave);
        yield return Esperar(() => !PantallaCuenta.Abierta);
        Assert.AreEqual(id, Sesion.JugadorId, "Entrar no recuperó la cuenta");
        Assert.AreEqual(nombre, Partida.Actual.nombre);

        // Cerrar sesión: al volver a arrancar pide correo y contraseña.
        bool fuera = false;
        Sesion.Salir(() => fuera = true);
        yield return Esperar(() => fuera);
        ok = null; Sesion.Iniciar((r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsFalse(ok.Value);
        Assert.IsTrue(Sesion.PideEntrar, "Tras cerrar sesión debería pedir entrar");

        // Borrar la cuenta.
        ok = null; Sesion.Entrar(correo, clave, (r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true);
        ok = null;
        Sesion.Borrar("mala", "", (r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsFalse(ok.Value, "Borró con una contraseña mala");
        ok = null; Sesion.Borrar(clave, "", (r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsTrue(ok == true, "No borró la cuenta");
        ok = null; Sesion.Entrar(correo, clave, (r, e) => ok = r);
        yield return Esperar(() => ok.HasValue);
        Assert.IsFalse(ok.Value, "La cuenta borrada sigue entrando");
    }
}
