using System;
using System.Collections.Generic;

/// Mensajes JSON de la API (api/src/rutas). Campos con el mismo nombre que en el JSON (JsonUtility).
public static class MensajesApi
{
    /// Recursos (en los escondites), el orco jefe y las herramientas de saqueo (unidades).
    [Serializable]
    public class Inventario
    {
        public int moneda, madera, piedra, hierro, hueso, orco_jefe;
        public int farol, cuarta_accion, pico, bomba, sabueso, senuelo;
    }
    [Serializable] public class Par { public string clave, valor; }

    [Serializable]
    public class Jugador
    {
        public string id, nombre, modelo, estandarte, escudo_hasta;
        public int puntuacion;
        public bool registrada;
        public Inventario inventario;
        /// Capacidad de los escondites y lo guardado (si guardado llega a capacidad, no entra más).
        public int capacidad, guardado;
        public List<Par> progreso;
        /// Saqueos de hoy (la API mantiene el nombre de la primera versión).
        public int asaltos_hoy, max_asaltos_dia;
    }

    [Serializable] public class PeticionSesion { public string token_dispositivo; }
    [Serializable] public class RespuestaSesion { public string token; public bool nuevo; public Jugador jugador; }
    [Serializable] public class RespuestaJugador { public Jugador jugador; public int perdido; }
    [Serializable] public class PeticionPersonaje { public string nombre, modelo; }
    [Serializable] public class Error { public string error, mensaje; }

    [Serializable] public class PeticionFuerte { public DisenoFuerte diseno; }
    [Serializable] public class RespuestaFuerte { public DisenoFuerte diseno; public Jugador jugador; }
    [Serializable] public class EsconditeReparto { public int x, z; public Contenido contenido; }
    [Serializable] public class PeticionReparto { public Contenido central; public List<EsconditeReparto> escondites; }

    [Serializable] public class Recursos { public int moneda, madera, piedra, hierro, hueso; }
    [Serializable] public class PeticionMazmorra { public bool jefe_muerto; public Recursos recursos; public int mazmorra = 1; }
    [Serializable] public class RespuestaMazmorra { public bool primera_vez_jefe; public int perdido; public Jugador jugador; }
    [Serializable] public class PeticionPueblo { public Recursos recursos; }

    // ---------- saqueo (08-formato-y-pvp.md §4) ----------

    [Serializable] public class RivalSaqueo { public string nombre, estandarte, tramo; public int puntuacion, piezas, escondites; public bool prueba; }
    [Serializable] public class HerramientaCantidad { public string id; public int cantidad; }
    [Serializable]
    public class RespuestaPreparar
    {
        public string saqueo_id;
        public RivalSaqueo rival;
        public int tasa, max_herramientas, saqueos_hoy, max_saqueos_dia;
        public List<HerramientaCantidad> herramientas;
    }
    [Serializable] public class PeticionEmpezar { public string saqueo_id; public List<string> herramientas; }
    [Serializable]
    public class RespuestaEmpezar
    {
        public string saqueo_id;
        public DisenoFuerte diseno;
        public float exploracion_segundos, mirar_segundos;
        public int acciones;
        public List<string> herramientas;
        public Jugador jugador;
    }
    [Serializable] public class PeticionMirar { public string saqueo_id; public int x, z; public float t; }
    [Serializable] public class RespuestaMirar { public string respuesta; }
    [Serializable] public class Casilla { public int x, z; }
    /// Una entrada de la exploración: posición (en casillas, con decimales) y, si golpeó, el muro.
    /// "golpea" dice si "golpe" va de verdad (JsonUtility no puede dejar "golpe" sin mandar).
    [Serializable] public class EntradaSaqueo { public float t, x, z; public bool golpea, bomba; public Casilla golpe; }
    [Serializable] public class PeticionResolver { public string saqueo_id; public List<Casilla> marcas; public List<EntradaSaqueo> entradas; }
    [Serializable] public class MarcaResultado { public int x, z; public string resultado, tipo; public Recursos botin; }
    [Serializable]
    public class RespuestaResolver
    {
        public bool prueba, tarde;
        public List<MarcaResultado> marcas;
        public Recursos botin, botin_prueba;
        public int perdido, descartadas, puntos_antes, puntos_despues, tasa_al_defensor;
        public List<Casilla> rotos;
        public Jugador jugador;
    }
    [Serializable] public class CasillaInforme { public int x, z; public string resultado; public Recursos botin; }
    /// Lo que ve el defensor de un saqueo recibido (08 §4.5): roto, mirado, marcado y encontrado.
    [Serializable]
    public class Informe
    {
        public string id, atacante, creado;
        public bool visto;
        public List<Casilla> rotos;
        public List<CasillaInforme> miradas, marcas;
        public int encontrado, puntos, tasa;
        public Recursos botin;
    }
    [Serializable] public class RespuestaInformes { public List<Informe> informes; }
    [Serializable] public class FilaHistorial { public string id, rival, creado; public int encontrado, marcas, puntos; public bool prueba; public Recursos botin; }
    [Serializable] public class RespuestaHistorial { public List<FilaHistorial> hechos, recibidos; }
    [Serializable] public class PeticionFabricar { public string id; }

    [Serializable] public class FilaRanking { public int puesto; public string id, nombre; public int puntuacion; }
    [Serializable] public class RespuestaRanking { public List<FilaRanking> mejores; public int tu_puesto, tu_puntuacion; }

    // ---------- cuentas y estado ----------

    [Serializable] public class PeticionCuenta { public string correo, contrasena; }
    [Serializable] public class Cuenta { public string tipo, correo, creado; public bool verificado; }
    [Serializable] public class RespuestaCuenta { public string token; public Cuenta cuenta; public Jugador jugador; }
    [Serializable] public class RespuestaCuentaSola { public Cuenta cuenta; }
    [Serializable] public class PeticionBorrar { public string contrasena, confirmar; }
    [Serializable] public class PeticionCorreo { public string correo; }

    [Serializable] public class AvisoApi { public string id, titulo, texto, creado; }
    [Serializable] public class Estado { public int version_minima; public string mantenimiento; public List<AvisoApi> avisos; }

    public static Recursos DeInventario(DatosInventario d) =>
        new Recursos { moneda = d.monedas, madera = d.madera, piedra = d.piedra, hierro = d.hierro, hueso = d.hueso };

    /// "6 monedas, 2 madera" o "nada".
    public static string Texto(Recursos r)
    {
        if (r == null) return "nada";
        var c = new Contenido { moneda = r.moneda, madera = r.madera, piedra = r.piedra, hierro = r.hierro, hueso = r.hueso };
        return c.Unidades > 0 ? c.ToString() : "nada";
    }
}
