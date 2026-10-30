using UnityEngine;

/// La API manda; la partida local es la caché (tarea 6.8).
/// - Al recibir el jugador, inventario, unidades, puntuación y progreso se toman de la API
///   (más lo recogido en el pueblo que aún no se ha enviado; las pociones solo existen en el móvil).
/// - Lo recogido en el pueblo se envía en lotes cada 5 s (POST /pueblo/botin, hasta la fase 7).
/// - Al salir de la mazmorra se envía su botín (POST /mazmorra/resultado).
/// - "Listo" en el modo construir envía el fuerte (PUT /fuerte).
/// - El saqueo lo llevan Saqueo y PantallaPrepararSaqueo; aquí, el reparto de escondites y los informes recibidos.
public class Sincronizacion : MonoBehaviour
{
    const float CadaSegundos = 5f;

    static Sincronizacion _instancia;
    static DatosInventario _pendientePueblo = new DatosInventario();
    static bool _enviando;
    static bool _lleno;
    float _siguiente;

    /// Lo recogido en el pueblo que aún no ha llegado a la API.
    public static DatosInventario PendientePueblo => _pendientePueblo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        var go = new GameObject("Sincronizacion");
        DontDestroyOnLoad(go);
        _instancia = go.AddComponent<Sincronizacion>();
        Transicion.AlLlegar += AlCambiarDeEscena;
        EditorFuerte.AlGuardar += EnviarFuerte;
    }

    void Update()
    {
        if (Time.unscaledTime < _siguiente) return;
        _siguiente = Time.unscaledTime + CadaSegundos;
        EnviarPueblo();
    }

    void OnApplicationPause(bool pausa) { if (pausa) EnviarPueblo(); }

    // ---------- jugador ----------

    /// Copia a la partida lo que dice la API.
    public static void AplicarJugador(MensajesApi.Jugador j)
    {
        if (j == null) return;
        var p = Partida.Actual;
        var inv = p.inventario;
        var s = j.inventario ?? new MensajesApi.Inventario();
        inv.monedas = s.moneda + _pendientePueblo.monedas;
        inv.madera = s.madera + _pendientePueblo.madera;
        inv.piedra = s.piedra + _pendientePueblo.piedra;
        inv.hierro = s.hierro + _pendientePueblo.hierro;
        inv.hueso = s.hueso + _pendientePueblo.hueso;
        // Unidades: orco jefe y herramientas de saqueo (08 §5).
        void Unidades(string tipo, int n) { p.unidades.RemoveAll(u => u.tipo == tipo); for (int i = 0; i < n; i++) p.unidades.Add(new DatosUnidad { tipo = tipo }); }
        Unidades(Jefe.TipoUnidad, s.orco_jefe);
        Unidades("farol", s.farol); Unidades("cuarta_accion", s.cuarta_accion); Unidades("pico", s.pico);
        Unidades("bomba", s.bomba); Unidades("sabueso", s.sabueso); Unidades("senuelo", s.senuelo);
        p.capacidad = j.capacidad; p.guardado = j.guardado;
        p.estandarte = j.estandarte ?? "";
        // Escondites llenos: se avisa una vez cada vez que se llenan (08 §3.1).
        bool lleno = j.capacidad > 0 && j.guardado >= j.capacidad;
        if (lleno && !_lleno) Avisos.Mostrar("Escondites llenos: construye o gasta", 3f);
        _lleno = lleno;
        p.puntuacion = j.puntuacion;
        if (!string.IsNullOrEmpty(j.nombre)) p.nombre = j.nombre;
        if (!string.IsNullOrEmpty(j.modelo)) p.modelo = j.modelo;
        p.progreso.Clear();
        if (j.progreso != null) foreach (var x in j.progreso) p.progreso.Add(new DatosPartida.Par { clave = x.clave, valor = x.valor });
        p.asaltosHoy = j.asaltos_hoy;
        Partida.Cambiado();
    }

    /// GET /jugador y GET /fuerte: deja la partida igual que la API.
    public static void Refrescar(System.Action<bool> fin = null)
    {
        if (!Sesion.Conectado) { fin?.Invoke(false); return; }
        ApiCliente.Get("/jugador", r =>
        {
            if (!r.Ok) { fin?.Invoke(false); return; }
            AplicarJugador(r.Leer<MensajesApi.RespuestaJugador>().jugador);
            ApiCliente.Get("/fuerte", rf =>
            {
                if (rf.Ok) { Partida.Actual.fuerte = rf.Leer<MensajesApi.RespuestaFuerte>().diseno ?? new DisenoFuerte(); Partida.Cambiado(); }
                fin?.Invoke(rf.Ok);
            });
        });
    }

    // ---------- pueblo ----------

    /// Lo llama el jugador al recoger algo en el pueblo.
    public static void AnotarPueblo(Recurso r, int n)
    {
        if (Configuracion.ModoLocal || r == Recurso.Pocion) return;
        _pendientePueblo[r] += n;
    }

    static void EnviarPueblo()
    {
        if (!Sesion.Conectado || _enviando) return;
        var envio = _pendientePueblo.Copia();
        envio.pociones = 0;
        bool hay = false;
        foreach (var r in DatosInventario.Todos) if (envio[r] > 0) hay = true;
        if (!hay) return;
        _enviando = true;
        ApiCliente.Post("/pueblo/botin", new MensajesApi.PeticionPueblo { recursos = MensajesApi.DeInventario(envio) }, res =>
        {
            _enviando = false;
            if (res.codigo == 0) return;   // sin conexión: se reintenta
            // Enviado (o rechazado por límite): deja de estar pendiente.
            foreach (var r in DatosInventario.Todos) _pendientePueblo[r] = Mathf.Max(0, _pendientePueblo[r] - envio[r]);
            if (res.Ok) AplicarJugador(res.Leer<MensajesApi.RespuestaJugador>().jugador);
            else { Avisos.Mostrar(res.Mensaje, 2f); Refrescar(); }
        });
    }

    // ---------- mazmorra ----------

    static void AlCambiarDeEscena(string escena)
    {
        if (Viaje.escenaAnterior == "Pueblo") EnviarPueblo();
        if (NumeroMazmorra(Viaje.escenaAnterior) > 0) EnviarMazmorra(NumeroMazmorra(Viaje.escenaAnterior));
        if (escena == "Pueblo" && Sesion.Conectado) { Balance.Descargar(); PedirInformes(); }
        if (escena == "Fuerte" && Sesion.Conectado) PedirInformes();
        // Antes de construir, lo último de la API (inventario y fuerte).
        if (escena == "Fuerte") Refrescar(_ => { if (Fuerte.Actual) Fuerte.Actual.Reconstruir(); });
    }

    /// "Mazmorra2" → 2; 0 si no es una mazmorra. Las plantas de la torre son "Mazmorra3", "Mazmorra3b" y "Mazmorra3c".
    public static int NumeroMazmorra(string escena)
    {
        if (string.IsNullOrEmpty(escena) || !escena.StartsWith("Mazmorra") || escena.Length < 9) return 0;
        return char.IsDigit(escena[8]) ? escena[8] - '0' : 0;
    }

    static void EnviarMazmorra(int numero)
    {
        if (!Sesion.Conectado || Partida.InventarioAlEntrar == null) return;
        var ganado = new DatosInventario();
        foreach (var r in DatosInventario.Todos) ganado[r] = Mathf.Max(0, Partida.Inventario[r] - Partida.InventarioAlEntrar[r]);
        ganado.pociones = 0;
        var pet = new MensajesApi.PeticionMazmorra { jefe_muerto = Partida.JefeMuertoEnEntrada, recursos = MensajesApi.DeInventario(ganado), mazmorra = numero };
        Partida.TerminarEntrada();
        ApiCliente.Post("/mazmorra/resultado", pet, r =>
        {
            if (r.Ok) AplicarJugador(r.Leer<MensajesApi.RespuestaMazmorra>().jugador);
            else { Avisos.Mostrar("No se pudo guardar el botín: " + r.Mensaje, 3f); Refrescar(); }
        });
    }

    // ---------- fuerte ----------

    static void EnviarFuerte(DisenoFuerte diseno)
    {
        if (!Sesion.Conectado) return;
        ApiCliente.Put("/fuerte", new MensajesApi.PeticionFuerte { diseno = diseno }, r =>
        {
            if (r.Ok)
            {
                var f = r.Leer<MensajesApi.RespuestaFuerte>();
                Partida.Actual.fuerte = f.diseno;
                AplicarJugador(f.jugador);
                Avisos.Mostrar("Fuerte guardado en el servidor", 1.5f);
            }
            else
            {
                Avisos.Mostrar("El servidor no aceptó el fuerte: " + r.Mensaje, 3f);
                Refrescar(_ => { if (Fuerte.Actual) Fuerte.Actual.Reconstruir(); });
            }
        });
    }

    // ---------- escondites ----------

    /// PUT /fuerte/escondites: el reparto hecho a mano (PantallaReparto). "fin(ok, mensaje)".
    public static void EnviarReparto(DisenoFuerte d, System.Action<bool, string> fin)
    {
        var pet = new MensajesApi.PeticionReparto { central = d.central, escondites = new System.Collections.Generic.List<MensajesApi.EsconditeReparto>() };
        var cat = CatalogoPiezas.Instancia;
        foreach (var q in d.piezas)
            if (cat.Buscar(q.tipo)?.categoria == CatalogoPiezas.Categoria.Escondite)
                pet.escondites.Add(new MensajesApi.EsconditeReparto { x = q.x, z = q.z, contenido = q.contenido });
        ApiCliente.Put("/fuerte/escondites", pet, r =>
        {
            if (r.Ok) { var f = r.Leer<MensajesApi.RespuestaFuerte>(); Partida.Actual.fuerte = f.diseno; AplicarJugador(f.jugador); }
            fin?.Invoke(r.Ok, r.Ok ? "" : r.Mensaje);
        });
    }

    // ---------- informes de saqueo ----------

    /// Informes de saqueos recibidos sin ver (se rellena al volver al pueblo o al fuerte).
    public static System.Collections.Generic.List<MensajesApi.Informe> InformesNuevos { get; private set; } = new System.Collections.Generic.List<MensajesApi.Informe>();
    public static event System.Action AlLlegarInformes;

    public static void PedirInformes(System.Action fin = null)
    {
        if (!Sesion.Conectado) { fin?.Invoke(); return; }
        ApiCliente.Get("/saqueo/informes", r =>
        {
            InformesNuevos = new System.Collections.Generic.List<MensajesApi.Informe>();
            if (r.Ok)
                foreach (var i in r.Leer<MensajesApi.RespuestaInformes>().informes ?? new System.Collections.Generic.List<MensajesApi.Informe>())
                    if (!i.visto) InformesNuevos.Add(i);
            if (InformesNuevos.Count > 0) AlLlegarInformes?.Invoke();
            fin?.Invoke();
        });
    }

    /// El defensor ya ha visto sus informes.
    public static void InformesVistos()
    {
        InformesNuevos.Clear();
        if (Sesion.Conectado) ApiCliente.Post("/saqueo/informes/visto", null, null);
    }
}
