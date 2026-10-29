using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

/// Crea los prefabs del juego en Assets/Prefabs a partir de los modelos de Kenney.
/// Las escenas y la paleta de mapas usan estos prefabs; rehacerlos actualiza todo.
public static class CrearPrefabs
{
    public const string Carpeta = "Assets/Prefabs/";
    public const string Dungeon = "Assets/Kenney/MiniDungeon/";
    public const string Natura = "Assets/Kenney/NatureKit/";
    public const string Controles = "Assets/Kenney/MobileControls/";
    public const string Anim = "Assets/Animacion/";

    [MenuItem("Prototipo/Crear prefabs")]
    public static void Crear()
    {
        Directory.CreateDirectory(Carpeta);
        foreach (var f in new[] { "character-human.fbx", "character-orc.fbx" })
            AssetDatabase.ImportAsset(Dungeon + f, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var moneda = CrearRecogible("Moneda", Dungeon + "coin.fbx", 0.35f, 0.25f, Recurso.Moneda);
        var pocion = CrearRecogible("Pocion", Dungeon + "potion.fbx", 0.6f, 0f, Recurso.Pocion);
        var madera = CrearRecogible("Madera", Natura + "log.fbx", 0.55f, 0.05f, Recurso.Madera);
        var piedra = CrearRecogible("Piedra", Natura + "stone_smallA.fbx", 0.7f, 0.05f, Recurso.Piedra);
        var hierro = CrearRecogible("Hierro", Dungeon + "rocks.fbx", 0.45f, 0.05f, Recurso.Hierro);
        var hueso = CrearRecogible("Hueso", Natura + "stone_smallFlatB.fbx", 0.6f, 0.05f, Recurso.Hueso);
        CrearCatalogo(moneda, madera, piedra, hierro, hueso, pocion);
        CrearFogata();
        CrearJugador();
        CrearOrco(moneda);
        CrearPuerta();
        CrearCasa();
        CrearInterfaz();
        AssetDatabase.SaveAssets();
        Debug.Log("Prefabs creados en " + Carpeta);
    }

    public static GameObject Cargar(string nombre) => AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + nombre + ".prefab");

    static GameObject Guardar(GameObject g, string nombre)
    {
        var p = PrefabUtility.SaveAsPrefabAsset(g, Carpeta + nombre + ".prefab");
        Object.DestroyImmediate(g);
        return p;
    }

    // ---------- prefabs ----------

    static GameObject CrearRecogible(string nombre, string modelo, float escala, float altura, Recurso recurso)
    {
        var g = new GameObject(nombre);
        var m = Poner(modelo, g.transform, new Vector3(0f, altura, 0f), 0f);
        m.transform.localScale = Vector3.one * escala;
        g.AddComponent<Recogible>().recurso = recurso;
        return Guardar(g, nombre);
    }

    public const string RutaCatalogo = "Assets/Resources/Catalogo.asset";

    static void CrearCatalogo(GameObject moneda, GameObject madera, GameObject piedra, GameObject hierro, GameObject hueso, GameObject pocion)
    {
        Directory.CreateDirectory("Assets/Resources");
        var c = AssetDatabase.LoadAssetAtPath<Catalogo>(RutaCatalogo);
        if (!c) { c = ScriptableObject.CreateInstance<Catalogo>(); AssetDatabase.CreateAsset(c, RutaCatalogo); }
        c.moneda = moneda; c.madera = madera; c.piedra = piedra; c.hierro = hierro; c.hueso = hueso; c.pocion = pocion;
        EditorUtility.SetDirty(c);
    }

    static void CrearFogata()
    {
        var g = new GameObject("Fogata");
        g.AddComponent<Fogata>();
        Guardar(g, "Fogata");
    }

    static void CrearJugador()
    {
        var ctrl = CrearControlador(Dungeon + "character-human.fbx", Anim + "humano.controller");
        var j = new GameObject("Jugador");
        var visual = Poner(Dungeon + "character-human.fbx", j.transform, Vector3.zero, 0f);
        visual.name = "Modelo";
        var an = Animador(visual);
        an.runtimeAnimatorController = ctrl;
        var cc = j.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.4f, 0f); cc.height = 0.75f; cc.radius = 0.2f; cc.stepOffset = 0.2f;
        j.AddComponent<Vida>().maxima = 6;
        j.AddComponent<Inventario>();
        j.AddComponent<ControlJugador>().animador = an;
        j.AddComponent<Regeneracion>();
        j.AddComponent<EfectoDano>().alturaNumero = 1.05f;
        Guardar(j, "Jugador");
    }

    static void CrearOrco(GameObject moneda)
    {
        var ctrl = CrearControlador(Dungeon + "character-orc.fbx", Anim + "orco.controller");
        var e = new GameObject("Orco");
        var v = Poner(Dungeon + "character-orc.fbx", e.transform, Vector3.zero, 0f);
        v.name = "Modelo";
        var an = Animador(v);
        an.runtimeAnimatorController = ctrl;
        var cc = e.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.4f, 0f); cc.height = 0.78f; cc.radius = 0.22f;
        e.AddComponent<Vida>().maxima = 3;
        var en = e.AddComponent<Enemigo>();
        en.animador = an;
        en.sueltaAlMorir = moneda;
        e.AddComponent<EfectoDano>().alturaNumero = 1.05f;
        Guardar(e, "Orco");
    }

    static void CrearPuerta()
    {
        var g = new GameObject("Puerta");
        g.AddComponent<Puerta>();
        Guardar(g, "Puerta");
    }

    /// Casa de 2x2: bloque de muros de Mini Dungeon con armazón de madera encima.
    static void CrearCasa()
    {
        var g = new GameObject("Casa");
        foreach (var x in new[] { -0.5f, 0.5f })
            foreach (var z in new[] { -0.5f, 0.5f })
            {
                Poner(Dungeon + "wall.fbx", g.transform, new Vector3(x, 0f, z), 0f);
                Poner(Dungeon + "wood-structure.fbx", g.transform, new Vector3(x, 1.1f, z), 0f);
            }
        Poner(Dungeon + "banner.fbx", g.transform, new Vector3(-0.5f, 0.15f, -0.53f), 0f);
        Poner(Dungeon + "banner.fbx", g.transform, new Vector3(0.5f, 0.15f, -0.53f), 0f);
        Guardar(g, "Casa");
    }

    static void CrearInterfaz()
    {
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
        var inter = Boton("BotonInteractuar", canvasGo.transform, new Vector2(-380f, 150f), "icon_hand.png", BotonAccion.Tipo.Interactuar);
        var ctx = inter.AddComponent<BotonContexto>();
        ctx.icono = inter.transform.Find("Icono").GetComponent<Image>();
        // Mismo orden que Interactuable.Icono: Mano, Recoger, Puerta, Cofre, Tablon, Construir.
        ctx.sprites = new[] { "icon_hand", "icon_hand", "icon_key", "icon_money", "icon_star", "icon_wrench" }
            .Select(n => AssetDatabase.LoadAssetAtPath<Sprite>(Controles + n + ".png")).ToArray();
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
        hudGo.AddComponent<HUD>().texto = txt;
        Guardar(canvasGo, "Interfaz");
    }

    // ---------- ayudas ----------

    /// Devuelve el Animator del modelo importado; si el FBX no lo trae, lo añade en la raíz.
    public static Animator Animador(GameObject modelo)
    {
        var a = modelo.GetComponentInChildren<Animator>();
        if (!a) a = modelo.AddComponent<Animator>();
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        return a;
    }

    public static GameObject Poner(string ruta, Transform padre, Vector3 posLocal, float giroY)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (!prefab) { Debug.LogWarning("No encuentro " + ruta); var v = new GameObject(Path.GetFileNameWithoutExtension(ruta)); v.transform.SetParent(padre, false); return v; }
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = posLocal;
        g.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);
        return g;
    }

    /// Crea un AnimatorController con un estado por clip del FBX. Sin transiciones:
    /// el código elige el estado por nombre con CrossFade.
    public static AnimatorController CrearControlador(string rutaFbx, string rutaSalida)
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

    public static GameObject Imagen(string nombre, Transform padre, string rutaSprite, Vector2 ancla, Vector2 pos, Vector2 tam)
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

    static GameObject Boton(string nombre, Transform padre, Vector2 pos, string icono, BotonAccion.Tipo tipo)
    {
        var b = Imagen(nombre, padre, Controles + "button_circle.png", new Vector2(1f, 0f), pos, new Vector2(170f, 170f));
        var ic = Imagen("Icono", b.transform, Controles + icono, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 90f));
        ic.GetComponent<Image>().raycastTarget = false;
        b.AddComponent<BotonAccion>().tipo = tipo;
        return b;
    }
}
