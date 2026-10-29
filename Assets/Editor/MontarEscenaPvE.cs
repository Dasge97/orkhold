using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Monta la escena del PvE básico a partir de los modelos de Kenney.
/// Se ejecuta sola la primera vez que se abre el proyecto (si la escena no existe)
/// y también desde el menú Prototipo > Montar escena PvE.
[InitializeOnLoad]
public static class MontarEscenaPvE
{
    const string RutaEscena = "Assets/Scenes/PvE.unity";
    const string Dungeon = "Assets/Kenney/MiniDungeon/";
    const string Natura = "Assets/Kenney/NatureKit/";
    const string Controles = "Assets/Kenney/MobileControls/";
    const string Anim = "Assets/Animacion/";

    static MontarEscenaPvE()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(RutaEscena) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Dungeon + "character-human.fbx") == null) return;
            Montar();
        };
    }

    [MenuItem("Prototipo/Montar escena PvE")]
    public static void Montar()
    {
        // Los personajes se reimportan para asegurar que pasan por ImportadorKenney (rig genérico + clips en bucle).
        foreach (var f in new[] { "character-human.fbx", "character-orc.fbx" })
            AssetDatabase.ImportAsset(Dungeon + f, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Random.InitState(7);

        // --- Luz y ambiente ---
        var luz = new GameObject("Sol").AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        luz.shadows = LightShadows.Soft;
        luz.intensity = 1.1f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.72f, 0.85f, 0.95f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 14f;
        RenderSettings.fogEndDistance = 30f;

        // --- Mundo ---
        var mundo = new GameObject("Mundo");
        const int N = 22;                       // casillas por lado
        float mitad = N / 2f - 0.5f;
        var suelo = new GameObject("Suelo"); suelo.transform.SetParent(mundo.transform);
        for (int x = 0; x < N; x++)
            for (int z = 0; z < N; z++)
                Poner(Natura + "ground_grass.fbx", suelo.transform, new Vector3(x - mitad, 0f, z - mitad), Random.Range(0, 4) * 90f);

        // Camino de tierra que cruza el mapa.
        for (int z = 0; z < N; z++)
            Poner(Natura + "ground_pathStraight.fbx", suelo.transform, new Vector3(0f, 0.001f, z - mitad), 0f);

        // Un único collider plano para todo el suelo (más barato que uno por casilla).
        var col = suelo.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.25f, 0f);
        col.size = new Vector3(N, 0.5f, N);

        // Borde de árboles para que no se salga del mapa.
        var deco = new GameObject("Decoracion"); deco.transform.SetParent(mundo.transform);
        string[] arboles = { "tree_default.fbx", "tree_pineRoundA.fbx", "tree_pineRoundB.fbx", "tree_oak.fbx", "tree_fat.fbx" };
        for (int i = 0; i < N; i++)
            foreach (var p in new[] { new Vector3(i - mitad, 0f, -mitad), new Vector3(i - mitad, 0f, mitad), new Vector3(-mitad, 0f, i - mitad), new Vector3(mitad, 0f, i - mitad) })
            {
                var a = Poner(Natura + arboles[Random.Range(0, arboles.Length)], deco.transform, p, Random.Range(0f, 360f));
                Capsula(a, 0.3f, 2f);
            }

        // Árboles, rocas y flores sueltos (lejos del camino y de la mazmorra).
        string[] sueltos = { "tree_default.fbx", "tree_pineRoundC.fbx", "rock_largeA.fbx", "rock_largeB.fbx", "rock_tallA.fbx", "plant_bushLarge.fbx", "stump_round.fbx", "log.fbx" };
        string[] flores = { "flower_redA.fbx", "flower_yellowA.fbx", "flower_purpleA.fbx", "grass_large.fbx", "mushroom_redGroup.fbx", "plant_bushSmall.fbx" };
        for (int i = 0; i < 28; i++)
        {
            var p = PosLibre(mitad);
            var g = Poner(Natura + sueltos[Random.Range(0, sueltos.Length)], deco.transform, p, Random.Range(0f, 360f));
            Capsula(g, 0.35f, 2f);
        }
        for (int i = 0; i < 40; i++)
            Poner(Natura + flores[Random.Range(0, flores.Length)], deco.transform, PosLibre(mitad), Random.Range(0f, 360f));
        Poner(Natura + "campfire_stones.fbx", deco.transform, new Vector3(2f, 0f, -2f), 0f);

        // Mazmorra: una sala de 7x7 en la esquina noreste con una entrada al sur.
        var mazmorra = new GameObject("Mazmorra"); mazmorra.transform.SetParent(mundo.transform);
        Vector3 o = new Vector3(4f, 0.002f, 4f);
        for (int x = 0; x < 7; x++)
            for (int z = 0; z < 7; z++)
            {
                Poner(Dungeon + (Random.value < 0.15f ? "floor-detail.fbx" : "floor.fbx"), mazmorra.transform, o + new Vector3(x, 0f, z), 0f);
                bool borde = x == 0 || x == 6 || z == 0 || z == 6;
                bool puerta = z == 0 && x == 3;
                if (borde && !puerta)
                {
                    var w = Poner(Dungeon + "wall.fbx", mazmorra.transform, o + new Vector3(x, 0f, z), 0f);
                    Caja(w, new Vector3(1f, 1.1f, 1f), new Vector3(0f, 0.55f, 0f));
                }
                if (puerta) Poner(Dungeon + "gate.fbx", mazmorra.transform, o + new Vector3(x, 0f, z), 0f);
            }
        Capsula(Poner(Dungeon + "chest.fbx", mazmorra.transform, o + new Vector3(3f, 0f, 5f), 180f), 0.4f, 0.6f);
        Capsula(Poner(Dungeon + "barrel.fbx", mazmorra.transform, o + new Vector3(1f, 0f, 5f), 0f), 0.3f, 0.8f);
        Capsula(Poner(Dungeon + "barrel.fbx", mazmorra.transform, o + new Vector3(5f, 0f, 5f), 0f), 0.3f, 0.8f);
        Poner(Dungeon + "banner.fbx", mazmorra.transform, o + new Vector3(1f, 0f, 6f), 0f);
        Poner(Dungeon + "banner.fbx", mazmorra.transform, o + new Vector3(5f, 0f, 6f), 0f);
        Poner(Dungeon + "column.fbx", mazmorra.transform, o + new Vector3(1f, 0f, 1f), 0f);
        Poner(Dungeon + "column.fbx", mazmorra.transform, o + new Vector3(5f, 0f, 1f), 0f);

        // --- Recogibles ---
        var objetos = new GameObject("Objetos");
        var monedaPlantilla = CrearRecogible(Dungeon + "coin.fbx", objetos.transform, new Vector3(-99f, -99f, -99f), Recogible.Clase.Moneda);
        monedaPlantilla.name = "MonedaPlantilla"; monedaPlantilla.SetActive(false);
        for (int i = 0; i < 8; i++) CrearRecogible(Dungeon + "coin.fbx", objetos.transform, PosLibre(mitad) + Vector3.up * 0.3f, Recogible.Clase.Moneda);
        for (int i = 0; i < 3; i++) CrearRecogible(Dungeon + "potion.fbx", objetos.transform, PosLibre(mitad), Recogible.Clase.Pocion);
        CrearRecogible(Dungeon + "potion.fbx", objetos.transform, o + new Vector3(3f, 0f, 4f), Recogible.Clase.Pocion);

        // --- Jugador ---
        var ctrlHumano = CrearControlador(Dungeon + "character-human.fbx", Anim + "humano.controller");
        var jugador = new GameObject("Jugador");
        jugador.transform.position = new Vector3(0f, 0.05f, -6f);
        var visual = Poner(Dungeon + "character-human.fbx", jugador.transform, jugador.transform.position, 0f);
        visual.name = "Modelo";
        var animJ = Animador(visual);
        animJ.runtimeAnimatorController = ctrlHumano;
        var cc = jugador.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.4f, 0f); cc.height = 0.75f; cc.radius = 0.2f; cc.stepOffset = 0.2f;
        var vidaJ = jugador.AddComponent<Vida>(); vidaJ.maxima = 6;
        var inv = jugador.AddComponent<Inventario>();
        var ctrl = jugador.AddComponent<ControlJugador>();
        ctrl.animador = animJ;

        // --- Enemigos ---
        var ctrlOrco = CrearControlador(Dungeon + "character-orc.fbx", Anim + "orco.controller");
        var enemigos = new GameObject("Enemigos");
        Vector3[] sitios = { o + new Vector3(2f, 0f, 3f), o + new Vector3(4f, 0f, 3f), o + new Vector3(3f, 0f, 5f), new Vector3(-5f, 0f, 3f), new Vector3(-3f, 0f, 7f), new Vector3(6f, 0f, -4f) };
        foreach (var s in sitios)
        {
            var e = new GameObject("Orco");
            e.transform.SetParent(enemigos.transform);
            e.transform.position = s + Vector3.up * 0.05f;
            e.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var v = Poner(Dungeon + "character-orc.fbx", e.transform, e.transform.position, e.transform.eulerAngles.y);
            v.name = "Modelo";
            var animE = Animador(v);
            animE.runtimeAnimatorController = ctrlOrco;
            var ecc = e.AddComponent<CharacterController>();
            ecc.center = new Vector3(0f, 0.4f, 0f); ecc.height = 0.78f; ecc.radius = 0.22f;
            var vida = e.AddComponent<Vida>(); vida.maxima = 3;
            var en = e.AddComponent<Enemigo>();
            en.animador = animE;
            en.sueltaAlMorir = monedaPlantilla;
        }

        // --- Cámara ---
        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.72f, 0.85f, 0.95f);
        cam.gameObject.AddComponent<AudioListener>();
        var sigue = cam.gameObject.AddComponent<CamaraSigue>();
        sigue.objetivo = jugador.transform;
        cam.transform.position = jugador.transform.position + sigue.desplazamiento;
        ctrl.cameraRef = cam.transform;

        // --- Interfaz: joystick, botones y HUD ---
        var canvasGo = new GameObject("Interfaz");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var esc = canvasGo.AddComponent<CanvasScaler>();
        esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        esc.referenceResolution = new Vector2(1920f, 1080f);
        esc.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var pad = Imagen("Joystick", canvasGo.transform, Controles + "joystick_circle_pad_a.png", new Vector2(0f, 0f), new Vector2(230f, 230f), new Vector2(300f, 300f));
        var nub = Imagen("Bolita", pad.transform, Controles + "joystick_circle_nub_a.png", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
        nub.GetComponent<Image>().raycastTarget = false;
        var joy = pad.AddComponent<JoystickVirtual>();
        joy.bolita = nub.GetComponent<RectTransform>();
        joy.radio = 110f;

        Boton("BotonAtaque", canvasGo.transform, new Vector2(-200f, 190f), "icon_sword.png", BotonAccion.Tipo.Ataque);
        Boton("BotonInteractuar", canvasGo.transform, new Vector2(-380f, 150f), "icon_hand.png", BotonAccion.Tipo.Interactuar);
        Boton("BotonCorrer", canvasGo.transform, new Vector2(-180f, 370f), "icon_arrow.png", BotonAccion.Tipo.Correr);

        var hudGo = new GameObject("HUD");
        hudGo.transform.SetParent(canvasGo.transform, false);
        var rt = hudGo.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f); rt.sizeDelta = new Vector2(900f, 160f);
        var txt = hudGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 42; txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        var sombra = hudGo.AddComponent<Shadow>(); sombra.effectDistance = new Vector2(2f, -2f);
        var hud = hudGo.AddComponent<HUD>();
        hud.texto = txt; hud.vida = vidaJ; hud.inventario = inv;

        var ev = new GameObject("EventSystem");
        ev.AddComponent<EventSystem>();
        ev.AddComponent<StandaloneInputModule>();

        // --- Guardar y ajustes del proyecto ---
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        PlayerSettings.companyName = "Orkhold";
        PlayerSettings.productName = "Orkhold";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.orkhold.game");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        AssetDatabase.SaveAssets();
        Debug.Log("Escena PvE montada en " + RutaEscena);
    }

    // ---------- ayudas ----------

    /// Devuelve el Animator del modelo importado; si el FBX no lo trae, lo añade en la raíz.
    static Animator Animador(GameObject modelo)
    {
        var a = modelo.GetComponentInChildren<Animator>();
        if (!a) a = modelo.AddComponent<Animator>();
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return a;
    }

    static GameObject Poner(string ruta, Transform padre, Vector3 pos, float giroY)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (!prefab) { Debug.LogWarning("No encuentro " + ruta); return new GameObject(Path.GetFileNameWithoutExtension(ruta)); }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.transform.SetParent(padre, true);
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        return g;
    }

    static Vector3 PosLibre(float mitad)
    {
        for (int i = 0; i < 50; i++)
        {
            var p = new Vector3(Random.Range(-mitad + 1.5f, mitad - 1.5f), 0f, Random.Range(-mitad + 1.5f, mitad - 1.5f));
            bool enCamino = Mathf.Abs(p.x) < 0.8f;
            bool enMazmorra = p.x > 3f && p.z > 3f;
            bool enSalida = Vector3.Distance(p, new Vector3(0f, 0f, -6f)) < 2f;
            if (!enCamino && !enMazmorra && !enSalida) return p;
        }
        return new Vector3(-6f, 0f, -6f);
    }

    static void Capsula(GameObject g, float radio, float alto)
    {
        var c = g.AddComponent<CapsuleCollider>();
        c.radius = radio; c.height = alto; c.center = new Vector3(0f, alto / 2f, 0f);
    }

    static void Caja(GameObject g, Vector3 tam, Vector3 centro)
    {
        var c = g.AddComponent<BoxCollider>(); c.size = tam; c.center = centro;
    }

    static GameObject CrearRecogible(string ruta, Transform padre, Vector3 pos, Recogible.Clase clase)
    {
        var g = Poner(ruta, padre, pos, 0f);
        // Los modelos de moneda y poción de Kenney son casi tan grandes como el personaje; se reducen.
        g.transform.localScale = Vector3.one * (clase == Recogible.Clase.Moneda ? 0.35f : 0.6f);
        g.AddComponent<Recogible>().clase = clase;
        return g;
    }

    /// Crea un AnimatorController con un estado por clip del FBX. Sin transiciones:
    /// el código elige el estado por nombre con CrossFade.
    static AnimatorController CrearControlador(string rutaFbx, string rutaSalida)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(rutaSalida));
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(rutaSalida);
        var clips = AssetDatabase.LoadAllAssetsAtPath(rutaFbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
        var capa = ctrl.layers[0].stateMachine;
        AnimatorState primero = null;
        foreach (var c in clips)
        {
            var s = capa.AddState(c.name);
            s.motion = c;
            if (c.name == "idle" || primero == null) primero = s;
        }
        if (primero) capa.defaultState = primero;
        if (clips.Length == 0) Debug.LogWarning("El FBX no trae clips: " + rutaFbx);
        return ctrl;
    }

    static GameObject Imagen(string nombre, Transform padre, string rutaSprite, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        var g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        var rt = g.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancla; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var img = g.AddComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(rutaSprite);
        img.preserveAspect = true;
        return g;
    }

    static void Boton(string nombre, Transform padre, Vector2 pos, string icono, BotonAccion.Tipo tipo)
    {
        var b = Imagen(nombre, padre, Controles + "button_circle.png", new Vector2(1f, 0f), pos, new Vector2(170f, 170f));
        var ic = Imagen("Icono", b.transform, Controles + icono, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        ic.GetComponent<Image>().raycastTarget = false;
        b.AddComponent<BotonAccion>().tipo = tipo;
    }
}
