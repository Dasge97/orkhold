using System;
using System.Collections.Generic;

/// Mensajes JSON de la API (api/src/rutas). Campos con el mismo nombre que en el JSON (JsonUtility).
public static class MensajesApi
{
    [Serializable] public class Inventario { public int moneda, madera, piedra, hierro, hueso, orco_jefe; }
    [Serializable] public class Par { public string clave, valor; }

    [Serializable]
    public class Jugador
    {
        public string id, nombre, modelo;
        public int puntuacion;
        public Inventario inventario;
        public List<Par> progreso;
        public int asaltos_hoy, max_asaltos_dia;
    }

    [Serializable] public class PeticionSesion { public string token_dispositivo; }
    [Serializable] public class RespuestaSesion { public string token; public bool nuevo; public Jugador jugador; }
    [Serializable] public class RespuestaJugador { public Jugador jugador; }
    [Serializable] public class PeticionPersonaje { public string nombre, modelo; }
    [Serializable] public class Error { public string error, mensaje; }

    [Serializable] public class PeticionFuerte { public DisenoFuerte diseno; }
    [Serializable] public class RespuestaFuerte { public DisenoFuerte diseno; public Jugador jugador; }

    [Serializable] public class Recursos { public int moneda, madera, piedra, hierro, hueso; }
    [Serializable] public class PeticionMazmorra { public bool jefe_muerto; public Recursos recursos; }
    [Serializable] public class RespuestaMazmorra { public bool primera_vez_jefe; public Jugador jugador; }
    [Serializable] public class PeticionPueblo { public Recursos recursos; }

    [Serializable] public class Rival { public string nombre; public int puntuacion; public bool sistema; }
    [Serializable] public class RespuestaBuscar { public string asalto_id; public Rival rival; public DisenoFuerte diseno; public int asaltos_hoy; }
    [Serializable] public class PeticionResultado { public string asalto_id; public bool ganado; public int segundos, muros_rotos; }
    [Serializable]
    public class RespuestaResultado
    {
        public bool ganado;
        public Recursos recompensa;
        public int puntos_antes, puntos_despues;
        public Jugador jugador;
    }

    [Serializable] public class FilaRanking { public int puesto; public string id, nombre; public int puntuacion; }
    [Serializable] public class RespuestaRanking { public List<FilaRanking> mejores; public int tu_puesto, tu_puntuacion; }
    [Serializable] public class FilaHistorial { public string id, rival; public bool ganado; public int segundos, muros_rotos, puntos; public string creado; }
    [Serializable] public class RespuestaHistorial { public List<FilaHistorial> hechos, recibidos; }

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
}
