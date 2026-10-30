using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// Red del pueblo (02-arquitectura.md §2.6 y §3, fase 7). Solo el pueblo es compartido.
/// - Servidor dedicado (argumento -servidor): mueve a todos, lleva enemigos y botín y avisa a la API.
/// - Cliente: se conecta al servidor de mundo; si no hay respuesta en 6 s, juega solo (modo anfitrión).
/// - Anfitrión (sin servidor configurado o sin conexión): el mismo juego en local, sin red de verdad.
/// Al entrar crea el NetworkManager; al salir por una puerta lo cierra. Enemigos, monedas y pociones
/// horneados en la escena se cambian por sus versiones de red, que crea el servidor en el mismo sitio.
public class RedPueblo : MonoBehaviour
{
    public enum Modo { Ninguno, Anfitrion, Cliente, Servidor }
    public const float EsperaConexion = 6f;

    public static RedPueblo Actual { get; private set; }
    public Modo ModoActual { get; private set; } = Modo.Ninguno;
    public NetworkManager Red => _nm;

    /// Datos que manda cada cliente al conectarse.
    [Serializable]
    public class DatosConexion { public string token, nombre, modelo, llegarA; }

    /// Lo que el servidor sabe de cada cliente.
    public class Cliente { public DatosConexion datos; public string jugadorId = ""; }
    public readonly Dictionary<ulong, Cliente> Clientes = new Dictionary<ulong, Cliente>();

    struct Sitio { public GameObject prefab; public Vector3 pos; public Quaternion rot; public bool patrulla; public Vector3 punto; public int cantidad; }
    readonly List<Sitio> _sitios = new List<Sitio>();

    NetworkManager _nm;
    PrefabsRed _prefabs;
    bool _saliendo;

    public static bool EsServidorDedicado => Configuracion.Bandera("-servidor")
#if UNITY_SERVER
        || true
#endif
        ;

    void Awake()
    {
        Actual = this;
        _prefabs = PrefabsRed.Instancia;
        RecogerSitios();
    }

    void Start()
    {
        if (Configuracion.Bandera("-bot")) gameObject.AddComponent<BotRed>();
        CrearRed();
        if (EsServidorDedicado) Empezar(Modo.Servidor);
        else if (string.IsNullOrEmpty(Configuracion.HostMundo)) Empezar(Modo.Anfitrion);
        else StartCoroutine(Conectar());
    }

    void OnDestroy()
    {
        if (Actual == this) Actual = null;
        Cerrar();
    }

    // ---------- arranque ----------

    /// Enemigos y recogibles horneados: se apunta dónde estaban y se quitan (la red pone los suyos).
    void RecogerSitios()
    {
        foreach (var e in FindObjectsByType<Enemigo>())
        {
            _sitios.Add(new Sitio { prefab = _prefabs.orco, pos = e.transform.position, rot = e.transform.rotation, patrulla = e.patrulla, punto = e.puntoPatrulla });
            Destroy(e.gameObject);
        }
        foreach (var r in FindObjectsByType<Recogible>())
        {
            _sitios.Add(new Sitio { prefab = _prefabs.Recogible(r.recurso), pos = r.transform.position, rot = r.transform.rotation, cantidad = r.cantidad });
            Destroy(r.gameObject);
        }
        // El jugador horneado (si lo hay) sobra: cada cliente tiene el suyo de red.
        foreach (var j in FindObjectsByType<ControlJugador>()) if (!j.GetComponent<NetworkObject>()) Destroy(j.gameObject);
    }

    void CrearRed()
    {
        var go = new GameObject("NetworkManager");
        var transporte = go.AddComponent<UnityTransport>();
        _nm = go.AddComponent<NetworkManager>();
        // Al recargar el pueblo, el NetworkManager viejo aún existe un momento: este pasa a ser el principal.
        _nm.SetSingleton();
        _nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transporte,
            PlayerPrefab = _prefabs.jugador,
            EnableSceneManagement = false,
            ConnectionApproval = true,
            TickRate = 30,
        };
        foreach (var p in _prefabs.Todos()) _nm.AddNetworkPrefab(p);
        _nm.ConnectionApprovalCallback = Aprobar;
        _nm.OnServerStarted += AlEmpezarServidor;
        _nm.OnClientDisconnectCallback += AlDesconectar;
        _nm.OnClientConnectedCallback += id => Debug.Log($"Red: conectado cliente {id} (modo {ModoActual})");
        var datos = new DatosConexion
        {
            token = Sesion.Token, nombre = Partida.Actual.nombre, modelo = Partida.Actual.modelo, llegarA = Viaje.llegarA ?? "",
        };
        Viaje.llegarA = "";
        _nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(datos));
    }

    void Empezar(Modo modo)
    {
        ModoActual = modo;
        var t = (UnityTransport)_nm.NetworkConfig.NetworkTransport;
        switch (modo)
        {
            case Modo.Servidor:
                t.SetConnectionData("0.0.0.0", Configuracion.PuertoMundo, "0.0.0.0");
                _nm.StartServer();
                Debug.Log($"Red: servidor de mundo escuchando en UDP {Configuracion.PuertoMundo}");
                break;
            case Modo.Anfitrion:
                // Sin red de verdad: solo escucha en este móvil.
                // Puerto al azar fuera de los rangos que Windows reserva (49152 en adelante).
                t.SetConnectionData("127.0.0.1", (ushort)UnityEngine.Random.Range(21000, 29000), "127.0.0.1");
                _nm.StartHost();
                break;
            case Modo.Cliente:
                t.SetConnectionData(ResolverHost(Configuracion.HostMundo), Configuracion.PuertoMundo);
                _nm.StartClient();
                break;
        }
    }

    static string ResolverHost(string host)
    {
        if (System.Net.IPAddress.TryParse(host, out _)) return host;
        try
        {
            foreach (var ip in System.Net.Dns.GetHostAddresses(host))
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ip.ToString();
        }
        catch (Exception e) { Debug.LogWarning("Red: no se resuelve " + host + ": " + e.Message); }
        return host;
    }

    IEnumerator Conectar()
    {
        Empezar(Modo.Cliente);
        float t = 0f;
        while (t < EsperaConexion && !_nm.IsConnectedClient) { t += Time.unscaledDeltaTime; yield return null; }
        if (_nm.IsConnectedClient) yield break;
        Debug.LogWarning("Red: sin respuesta del servidor de mundo; se juega solo");
        _nm.Shutdown();
        yield return null;
        Avisos.Mostrar("Sin conexión con el pueblo online: juegas solo", 3f);
        Empezar(Modo.Anfitrion);
    }

    void AlDesconectar(ulong id)
    {
        // NGO avisa también al cerrar la red (salir por una puerta): entonces no hay nada que hacer.
        if (_saliendo || !_nm) return;
        if (_nm.IsServer)
        {
            Clientes.Remove(id);
            Debug.Log($"Red: se fue el cliente {id}");
            return;
        }
        if (ModoActual != Modo.Cliente) return;
        // El servidor se cayó o nos echó: se sigue jugando solo.
        Debug.LogWarning("Red: desconectado del servidor de mundo");
        StartCoroutine(PasarAAnfitrion());
    }

    IEnumerator PasarAAnfitrion()
    {
        _nm.Shutdown();
        yield return null;
        Avisos.Mostrar("Se perdió la conexión con el pueblo online: juegas solo", 3f);
        Empezar(Modo.Anfitrion);
    }

    // ---------- servidor ----------

    void Aprobar(NetworkManager.ConnectionApprovalRequest pet, NetworkManager.ConnectionApprovalResponse res)
    {
        DatosConexion d;
        try { d = JsonUtility.FromJson<DatosConexion>(Encoding.UTF8.GetString(pet.Payload ?? new byte[0])) ?? new DatosConexion(); }
        catch { d = new DatosConexion(); }
        var g = GestorEscena.Actual;
        Vector3 donde = Vector3.zero; float giro = 0f;
        if (g) g.Llegada(d.llegarA, out donde, out giro);
        // Varios a la vez en la fogata (servidor dedicado): un poco de separación.
        if (ModoActual == Modo.Servidor)
            donde += new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0f, UnityEngine.Random.Range(-0.4f, 0.4f));
        Debug.Log($"Red: aprobado {pet.ClientNetworkId} ({d.nombre}) llegarA='{d.llegarA}' en {donde} (gestor {(g ? g.gameObject.scene.name : "ninguno")})");
        var c = new Cliente { datos = d };
        Clientes[pet.ClientNetworkId] = c;
        if (ModoActual == Modo.Servidor && !string.IsNullOrEmpty(d.token))
            ApiServidor.ResolverJugador(d.token, id => { c.jugadorId = id; Debug.Log($"Red: cliente {pet.ClientNetworkId} es el jugador {id} ({d.nombre})"); });
        res.Approved = true;
        res.CreatePlayerObject = true;
        res.Position = donde;
        res.Rotation = Quaternion.Euler(0f, giro, 0f);
    }

    void AlEmpezarServidor()
    {
        Spawn(_prefabs.mundo, Vector3.zero, Quaternion.identity);
        foreach (var s in _sitios)
        {
            if (!s.prefab) continue;
            Spawn(s.prefab, s.pos, s.rot, s.cantidad, s.patrulla, s.punto);
        }
        // Lo que suelta un enemigo o un árbol también es de red.
        Catalogo.SoltarEnRed = (r, donde, n) =>
        {
            var p = _prefabs.Recogible(r);
            return p ? Spawn(p, donde, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), n) : null;
        };
    }

    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, int cantidad = 1, bool patrulla = false, Vector3 punto = default)
    {
        var g = Instantiate(prefab, pos, rot);
        var e = g.GetComponent<Enemigo>();
        if (e) { e.patrulla = patrulla; e.puntoPatrulla = punto; }
        var rec = g.GetComponent<Recogible>();
        if (rec) rec.cantidad = Mathf.Max(1, cantidad);
        g.GetComponent<NetworkObject>().Spawn(true);
        return g;
    }

    /// Lo recogido por el jugador de un cliente: a la API (servidor dedicado) y aviso a su móvil.
    public void ServidorRecogio(ulong cliente, Recurso r, int n)
    {
        if (Clientes.TryGetValue(cliente, out var c) && ApiServidor.Activa)
            ApiServidor.Sumar(c.jugadorId, r, n, "pueblo");
        else
            Debug.Log($"Red: cliente {cliente} recoge {n} {r} (sin API)");
    }

    // ---------- salir ----------

    /// Al cruzar una puerta del pueblo: se desconecta antes de cambiar de escena.
    public static void Salir()
    {
        if (Actual) Actual.Cerrar();
    }

    void Cerrar()
    {
        _saliendo = true;
        Catalogo.SoltarEnRed = null;
        if (!_nm) return;
        if (_nm.IsListening) _nm.Shutdown();
        Destroy(_nm.gameObject);
        _nm = null;
    }
}
