using UnityEngine;

/// La API manda; la partida local es la caché (tarea 6.8).
/// - Al recibir el jugador, inventario, unidades, puntuación y progreso se toman de la API
///   (más lo recogido en el pueblo que aún no se ha enviado; las pociones solo existen en el móvil).
/// - Lo recogido en el pueblo se envía en lotes cada 5 s (POST /pueblo/botin, hasta la fase 7).
/// - Al salir de la mazmorra se envía su botín (POST /mazmorra/resultado).
/// - "Listo" en el modo construir envía el fuerte (PUT /fuerte).
/// - El asalto busca rival y manda su resultado a la API.
public class Sincronizacion : MonoBehaviour
{
    const float CadaSegundos = 5f;

    static Sincronizacion _instancia;
    static DatosInventario _pendientePueblo = new DatosInventario();
    static bool _enviando;
    static string _asaltoId = "";
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
        // Se deciden al usarse: en modo local (sin URL de API) siguen haciendo lo de antes.
        BuscarRival.BuscarEnServidor = BuscarRivalEnServidor;
        Asalto.Procesar = ProcesarAsalto;
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
        p.unidades.RemoveAll(u => u.tipo == Jefe.TipoUnidad);
        for (int i = 0; i < s.orco_jefe; i++) p.unidades.Add(new DatosUnidad { tipo = Jefe.TipoUnidad });
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
        if (Viaje.escenaAnterior == "Mazmorra1") EnviarMazmorra();
        // Antes de construir, lo último de la API (inventario y fuerte).
        if (escena == "Fuerte") Refrescar(_ => { if (Fuerte.Actual) Fuerte.Actual.Reconstruir(); });
    }

    static void EnviarMazmorra()
    {
        if (!Sesion.Conectado || Partida.InventarioAlEntrar == null) return;
        var ganado = new DatosInventario();
        foreach (var r in DatosInventario.Todos) ganado[r] = Mathf.Max(0, Partida.Inventario[r] - Partida.InventarioAlEntrar[r]);
        ganado.pociones = 0;
        var pet = new MensajesApi.PeticionMazmorra { jefe_muerto = Partida.JefeMuertoEnEntrada, recursos = MensajesApi.DeInventario(ganado) };
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

    // ---------- asalto ----------

    static void BuscarRivalEnServidor()
    {
        if (!Sesion.Conectado) { Avisos.Mostrar("Sin conexión: el asalto necesita el servidor", 2.5f); return; }
        Avisos.Mostrar("Buscando rival...", 1.5f);
        ApiCliente.Post("/asalto/buscar", null, r =>
        {
            if (!r.Ok) { Avisos.Mostrar(r.Mensaje, 2.5f); return; }
            var b = r.Leer<MensajesApi.RespuestaBuscar>();
            _asaltoId = b.asalto_id;
            Partida.Actual.asaltosHoy = b.asaltos_hoy;
            BuscarRival.Ir(b.diseno, $"{b.rival.nombre} ({b.rival.puntuacion})");
        });
    }

    static void ProcesarAsalto(Asalto.Resultado res, System.Action<Asalto.Resultado> listo)
    {
        if (!Sesion.Conectado || string.IsNullOrEmpty(_asaltoId)) { listo(Asalto.RecompensaLocal(res)); return; }
        var pet = new MensajesApi.PeticionResultado { asalto_id = _asaltoId, ganado = res.ganado, segundos = res.segundos, muros_rotos = res.murosRotos };
        _asaltoId = "";
        ApiCliente.Post("/asalto/resultado", pet, r =>
        {
            if (r.Ok)
            {
                var x = r.Leer<MensajesApi.RespuestaResultado>();
                res.recompensa = new DatosInventario
                {
                    monedas = x.recompensa.moneda, madera = x.recompensa.madera, piedra = x.recompensa.piedra,
                    hierro = x.recompensa.hierro, hueso = x.recompensa.hueso,
                };
                res.puntosAntes = x.puntos_antes;
                res.puntosDespues = x.puntos_despues;
                AplicarJugador(x.jugador);
            }
            else
            {
                Avisos.Mostrar("No se pudo enviar el resultado: " + r.Mensaje, 3f);
                res.puntosAntes = res.puntosDespues = Partida.Actual.puntuacion;
            }
            listo(res);
        });
    }
}
