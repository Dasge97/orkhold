using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// Monta las escenas del juego a partir de los mapas de texto (Assets/Mapas/*.txt):
/// Pueblo, Mazmorra1, Fuerte y Asalto. Se ejecuta sola si falta la escena Pueblo y desde
/// el menú Prototipo > Montar escenas.
[InitializeOnLoad]
public static class MontarEscenas
{
    public const string CarpetaEscenas = "Assets/Scenes/";
    public const string CarpetaMapas = "Assets/Mapas/";
    public static readonly string[] Escenas = { "Pueblo", "Mazmorra1", "Fuerte", "Asalto" };

    enum Ambiente { Exterior, Mazmorra }

    static MontarEscenas()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isBatchMode) return;
            if (File.Exists(CarpetaEscenas + "Pueblo.unity") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CrearPrefabs.Dungeon + "character-human.fbx") == null) return;
            MontarTodas();
        };
    }

    [MenuItem("Prototipo/Montar escenas")]
    public static void MontarTodas()
    {
        CrearPrefabs.Crear();
        var paleta = CrearPaletaMapa.Crear();
        Directory.CreateDirectory(CarpetaEscenas);

        Montar("Pueblo", "pueblo", Ambiente.Exterior, paleta, r =>
        {
            Configurar(r, 'D', "mazmorra1", "Mazmorra1", "", true);
            Configurar(r, 'C', "asalto", "Asalto", "", false);
            PonerHuecoFuerte(r, paleta);
        });
        Montar("Mazmorra1", "catacumbas", Ambiente.Mazmorra, paleta, r =>
            Configurar(r, 'E', "salida", "Pueblo", "mazmorra1", true));
        Montar("Fuerte", "fuerte-base", Ambiente.Exterior, paleta, r =>
        {
            Configurar(r, 'E', "salida", "Pueblo", "fuerte", true);
            PonerPradera(r);
        });
        Montar("Asalto", "fuerte-base", Ambiente.Exterior, paleta, r =>
        {
            Configurar(r, 'E', "salida", "Pueblo", "asalto", true);
            PonerPradera(r);
        });

        if (File.Exists(CarpetaEscenas + "PvE.unity")) AssetDatabase.DeleteAsset(CarpetaEscenas + "PvE.unity");
        EditorBuildSettings.scenes = Escenas.Select(e => new EditorBuildSettingsScene(CarpetaEscenas + e + ".unity", true)).ToArray();
        AjustarCalidad();
        PlayerSettings.companyName = "Orkhold";
        PlayerSettings.productName = "Orkhold";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.orkhold.game");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(CarpetaEscenas + "Pueblo.unity");
        Debug.Log("Escenas montadas: " + string.Join(", ", Escenas));
    }

    static void Montar(string escena, string mapa, Ambiente amb, PaletaMapa paleta, System.Action<CargadorMapa.Resultado> extra)
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PonerAmbiente(amb);

        var texto = File.ReadAllText(CarpetaMapas + mapa + ".txt");
        var r = CargadorMapa.Cargar(texto, paleta, null, Instanciar);
        if (r.letrasDesconocidas.Count > 0) Debug.LogWarning($"{escena}: letras sin paleta: {new string(r.letrasDesconocidas.ToArray())}");
        CalcularLlegadas(r, paleta);
        extra?.Invoke(r);
        int trozos = CombinarMallas.Combinar(r.raiz.transform, escena, r.sueltas);
        MarcarEstatico(r.raiz);

        // Aparición: la letra de la cabecera. Si es una puerta, su casilla de llegada; si no, al sur de la casilla.
        var gestor = new GameObject("GestorEscena").AddComponent<GestorEscena>();
        char letraAparicion = r.mapa.Valor("aparicion", "F")[0];
        var marca = r.Buscar(letraAparicion);
        if (marca.HasValue)
        {
            var pu = marca.Value.entidad ? marca.Value.entidad.GetComponent<Puerta>() : null;
            gestor.aparicion = pu ? pu.llegada : marca.Value.posicion + new Vector3(0f, 0f, -1.2f);
            Vector3 dentro = gestor.aparicion - marca.Value.posicion; dentro.y = 0f;
            gestor.giroAparicion = dentro.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dentro).eulerAngles.y : 180f;
        }
        else Debug.LogWarning($"{escena}: no hay '{letraAparicion}' para la aparición");

        var jugador = (GameObject)PrefabUtility.InstantiatePrefab(CrearPrefabs.Cargar("Jugador"));
        jugador.transform.SetPositionAndRotation(gestor.aparicion + Vector3.up * 0.05f, Quaternion.Euler(0f, gestor.giroAparicion, 0f));

        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.3f; cam.farClipPlane = RenderSettings.fogEndDistance + 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        cam.gameObject.AddComponent<AudioListener>();
        var sigue = cam.gameObject.AddComponent<CamaraSigue>();
        sigue.objetivo = jugador.transform;
        cam.transform.position = jugador.transform.position + sigue.desplazamiento;
        cam.transform.LookAt(jugador.transform.position + Vector3.up * 0.5f);
        jugador.GetComponent<ControlJugador>().cameraRef = cam.transform;

        PrefabUtility.InstantiatePrefab(CrearPrefabs.Cargar("Interfaz"));
        var ev = new GameObject("EventSystem");
        ev.AddComponent<EventSystem>();
        ev.AddComponent<StandaloneInputModule>();

        EditorSceneManager.SaveScene(sc, CarpetaEscenas + escena + ".unity");
        Debug.Log($"Escena {escena} montada desde {mapa}.txt: trozos={trozos} sueltas={r.sueltas.Count} modelos={r.modelos} entidades={r.entidades.Count} colisiones unidas={r.colisionesUnidas}");
    }

    /// Sombras duras, cercanas y sin cascadas en todos los niveles de calidad: lo caro en móvil.
    static void AjustarCalidad()
    {
        int actual = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = 18f;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowCascades = 1;
            QualitySettings.pixelLightCount = 1;
            QualitySettings.antiAliasing = 0;
        }
        QualitySettings.SetQualityLevel(actual, false);
    }

    static GameObject Instanciar(GameObject m) => (GameObject)PrefabUtility.InstantiatePrefab(m);

    static void PonerAmbiente(Ambiente amb)
    {
        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        luz.shadows = LightShadows.Hard;
        luz.shadowStrength = 0.6f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        if (amb == Ambiente.Exterior)
        {
            luz.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.65f);
            RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.95f);
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 30f;
        }
        else
        {
            luz.intensity = 0.75f;
            luz.color = new Color(1f, 0.85f, 0.7f);
            RenderSettings.ambientLight = new Color(0.32f, 0.3f, 0.38f);
            RenderSettings.fogColor = new Color(0.06f, 0.05f, 0.08f);
            RenderSettings.fogStartDistance = 9f;
            RenderSettings.fogEndDistance = 20f;
        }
    }

    /// Para cada puerta, la casilla libre de al lado que queda más hacia el centro del mapa.
    static void CalcularLlegadas(CargadorMapa.Resultado r, PaletaMapa paleta)
    {
        var centro = new Vector2(r.mapa.Ancho / 2f, r.mapa.Alto / 2f);
        foreach (var m in r.marcas)
        {
            var pu = m.entidad ? m.entidad.GetComponent<Puerta>() : null;
            if (!pu) continue;
            Vector2Int mejor = new Vector2Int(-1, -1); float dMejor = float.MaxValue;
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                int c = m.columna + d.x, f = m.fila + d.y;
                if (!CargadorMapa.Transitable(r.mapa, paleta, c, f)) continue;
                float dist = Vector2.Distance(new Vector2(c, f), centro);
                if (dist < dMejor) { dMejor = dist; mejor = new Vector2Int(c, f); }
            }
            pu.llegada = mejor.x >= 0
                ? r.raiz.transform.TransformPoint(CargadorMapa.Casilla(r.mapa, mejor.x, mejor.y))
                : m.posicion + new Vector3(0f, 0f, -1f);
            EditorUtility.SetDirty(pu);
        }
    }

    /// Da id, destino y forma de uso a las puertas de una letra.
    static void Configurar(CargadorMapa.Resultado r, char letra, string id, string destino, string llegarA, bool alPisar)
    {
        int n = 0;
        foreach (var m in r.marcas.Where(m => m.letra == letra && m.entidad))
        {
            var pu = m.entidad.GetComponent<Puerta>();
            if (!pu) continue;
            pu.id = id; pu.destino = destino; pu.llegarA = llegarA; pu.alPisar = alPisar;
            if (!alPisar) pu.alcance = 1.5f;
            m.entidad.name = "Puerta " + id;
            EditorUtility.SetDirty(pu);
            n++;
        }
        if (n == 0) Debug.LogWarning($"No hay puerta '{letra}' para {id}");
    }

    /// En el pueblo, el fuerte base se pinta dentro del hueco marcado por '[' (noroeste) y ']' (sureste).
    /// Su entrada lleva a la escena Fuerte; al volver se aparece fuera, al oeste de la entrada.
    static void PonerHuecoFuerte(CargadorMapa.Resultado r, PaletaMapa paleta)
    {
        var no = r.Buscar('['); var se = r.Buscar(']');
        if (!no.HasValue || !se.HasValue) { Debug.LogWarning("El pueblo no tiene '[' y ']'"); return; }
        var baseMapa = CargadorMapa.Leer(File.ReadAllText(CarpetaMapas + "fuerte-base.txt"));
        int ancho = se.Value.columna - no.Value.columna + 1, alto = se.Value.fila - no.Value.fila + 1;
        if (ancho != baseMapa.Ancho || alto != baseMapa.Alto)
            Debug.LogWarning($"El hueco del fuerte mide {ancho}x{alto} y fuerte-base {baseMapa.Ancho}x{baseMapa.Alto}");
        var hueco = new GameObject("HuecoFuerte");
        hueco.transform.SetParent(r.raiz.transform, false);
        hueco.transform.position = no.Value.posicion + new Vector3(0f, 0f, -(baseMapa.Alto - 1));
        var rb = CargadorMapa.Colocar(baseMapa, paleta, hueco.transform, Instanciar);
        foreach (var g in rb.sueltas) r.sueltas.Add(g);
        Object.DestroyImmediate(rb.raiz.transform.Find("Suelo").GetComponent<BoxCollider>());
        foreach (var m in rb.marcas.Where(m => m.letra == 'E' && m.entidad))
        {
            var pu = m.entidad.GetComponent<Puerta>();
            pu.id = "fuerte"; pu.destino = "Fuerte"; pu.llegarA = ""; pu.alPisar = true;
            pu.llegada = m.posicion + Vector3.left;
            m.entidad.name = "Puerta fuerte";
            EditorUtility.SetDirty(pu);
        }
    }

    /// Hierba alrededor del fuerte para que no se vea el vacío.
    static void PonerPradera(CargadorMapa.Resultado r)
    {
        var g = CrearPrefabs.Poner(CrearPrefabs.Natura + "ground_grass.fbx", r.raiz.transform,
            new Vector3((r.mapa.Ancho - 1) / 2f, -0.03f, (r.mapa.Alto - 1) / 2f), 0f);
        g.name = "Pradera";
        g.transform.localScale = new Vector3(70f, 1f, 70f);
    }

    static void MarcarEstatico(GameObject raiz)
    {
        foreach (var nombre in new[] { "Suelo", "Piezas" })
        {
            var t = raiz.transform.Find(nombre);
            if (!t) continue;
            foreach (var hijo in t.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(hijo.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }
    }
}
