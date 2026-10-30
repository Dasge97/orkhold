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
        CrearEnemigos();
        CrearTrampa();
        CrearPiezasFuerte();
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
        c.fuertesPrueba = new[] { "facil", "medio", "dificil" }
            .Select(n => AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Mapas/fuertes-prueba/" + n + ".json")).ToArray();
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

    /// Los tres enemigos de 01-diseno.md §4.2 y el jefe de §4.3.
    static void CrearEnemigos()
    {
        var ctrlOrco = CrearControlador(Dungeon + "character-orc.fbx", Anim + "orco.controller");
        var ctrlHumano = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + "humano.controller");

        // Orco: vida 3, daño 1, persigue a 5 m, pega a 1 m cada 1,3 s, suelta 1-2 monedas.
        var orco = CrearEnemigo<Enemigo>("Orco", Dungeon + "character-orc.fbx", ctrlOrco, 1f, null, e =>
        {
            e.nombre = "Orco"; e.distanciaVer = 5f; e.distanciaPegar = 1f; e.velocidad = 2f; e.cadenciaAtaque = 1.3f; e.dano = 1;
            e.monedasMin = 1; e.monedasMax = 2; e.probabilidadExtra = 0f;
        }, 3);
        Guardar(orco, "Orco");

        // Arquero: vida 2, daño 1, se queda a 4 m y lanza cada 2 s, huye si te acercas; 1 moneda + 20 % hierro.
        var proyectil = CrearProyectil();
        var arquero = CrearEnemigo<EnemigoArquero>("Arquero", Dungeon + "character-orc.fbx", ctrlOrco, 1f, null, e =>
        {
            e.nombre = "Orco arquero"; e.distanciaVer = 7f; e.distanciaPegar = 1f; e.velocidad = 2f; e.dano = 1;
            e.distanciaIdeal = 4f; e.distanciaHuir = 2f; e.cadenciaDisparo = 2f; e.proyectil = proyectil;
            e.monedasMin = 1; e.monedasMax = 1; e.recursoExtra = Recurso.Hierro; e.probabilidadExtra = 0.2f;
        }, 2);
        var brazo = arquero.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "arm-right");
        var lanza = Poner(Dungeon + "weapon-spear.fbx", brazo ? brazo : arquero.transform, brazo ? new Vector3(0f, -0.12f, 0.06f) : new Vector3(0.2f, 0.3f, 0.1f), 0f);
        lanza.transform.localScale = Vector3.one * 0.85f;
        Guardar(arquero, "Arquero");

        // Esqueleto: humano oscuro, lento (1,5 m/s) y fuerte: vida 4, daño 2; 1 moneda + 30 % hueso.
        var esqueleto = CrearEnemigo<Enemigo>("Esqueleto", Dungeon + "character-human.fbx", ctrlHumano, 1f, MaterialOscuro(), e =>
        {
            e.nombre = "Esqueleto"; e.distanciaVer = 5f; e.distanciaPegar = 1f; e.velocidad = 1.5f; e.cadenciaAtaque = 1.6f; e.dano = 2;
            e.monedasMin = 1; e.monedasMax = 1; e.recursoExtra = Recurso.Hueso; e.probabilidadExtra = 0.3f;
        }, 4);
        Guardar(esqueleto, "Esqueleto");

        // Jefe: orco al 150 %, vida 15, daño 2, golpe en área cada 6 s.
        var jefe = CrearEnemigo<Jefe>("Jefe", Dungeon + "character-orc.fbx", ctrlOrco, 1.5f, null, e =>
        {
            e.nombre = "Orco jefe"; e.distanciaVer = 7f; e.distanciaPegar = 1.3f; e.velocidad = 1.8f; e.cadenciaAtaque = 1.6f; e.dano = 2;
            e.distanciaPerder = 12f; e.reaparecerSegundos = 0f; e.monedasMin = 5; e.monedasMax = 5;
        }, 15);
        Guardar(jefe, "Jefe");
    }

    static GameObject CrearEnemigo<T>(string nombre, string fbx, RuntimeAnimatorController ctrl, float escala, Material material,
                                       System.Action<T> numeros, int vida) where T : Enemigo
    {
        var e = new GameObject(nombre);
        var v = Poner(fbx, e.transform, Vector3.zero, 0f);
        v.name = "Modelo";
        v.transform.localScale = Vector3.one * escala;
        if (material) foreach (var r in v.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
        var an = Animador(v);
        an.runtimeAnimatorController = ctrl;
        var cc = e.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.4f * escala, 0f); cc.height = 0.78f * escala; cc.radius = 0.22f * escala;
        e.AddComponent<Vida>().maxima = vida;
        var en = e.AddComponent<T>();
        en.animador = an;
        numeros(en);
        e.AddComponent<EfectoDano>().alturaNumero = 1.05f * escala;
        return e;
    }

    /// Material del esqueleto: la textura de Kenney con un tono azul grisáceo de mazmorra.
    static Material MaterialOscuro()
    {
        const string ruta = Carpeta + "Materiales/Esqueleto.mat";
        Directory.CreateDirectory(Carpeta + "Materiales");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m) return m;
        var original = AssetDatabase.LoadAssetAtPath<GameObject>(Dungeon + "character-human.fbx").GetComponentInChildren<Renderer>().sharedMaterial;
        m = new Material(original) { color = new Color(0.55f, 0.6f, 0.78f) };
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static GameObject CrearProyectil()
    {
        var g = new GameObject("Proyectil");
        var l = Poner(Dungeon + "weapon-spear.fbx", g.transform, Vector3.zero, 0f);
        // La lanza es vertical: se tumba para que la punta mire hacia delante (+Z).
        l.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        l.transform.localPosition = new Vector3(0f, 0f, -0.3f);
        l.transform.localScale = Vector3.one * 0.7f;
        g.AddComponent<Proyectil>();
        return Guardar(g, "Proyectil");
    }

    public const string RutaCatalogoPiezas = "Assets/Resources/CatalogoPiezas.asset";

    /// Piezas del fuerte (01-diseno.md §5.1) y su catálogo.
    static void CrearPiezasFuerte()
    {
        // Muro de madera: valla alta del Nature Kit, centrada en la casilla y más alta que el original.
        var mm = new GameObject("PiezaMuroMadera");
        var valla = Poner(Natura + "fence_simpleHigh.fbx", mm.transform, new Vector3(0f, 0f, -0.47f), 0f);
        valla.transform.localScale = new Vector3(1f, 2.6f, 1f);
        var muroMadera = Guardar(mm, "PiezaMuroMadera");

        var mp = new GameObject("PiezaMuroPiedra");
        Poner(Dungeon + "wall.fbx", mp.transform, Vector3.zero, 0f);
        var muroPiedra = Guardar(mp, "PiezaMuroPiedra");

        var pu = new GameObject("PiezaPuerta");
        var gate = Poner(Dungeon + "gate.fbx", pu.transform, Vector3.zero, 0f);
        gate.transform.localScale = new Vector3(1.25f, 1.4f, 1.25f);
        var puerta = Guardar(pu, "PiezaPuerta");

        Directory.CreateDirectory("Assets/Resources");
        var c = AssetDatabase.LoadAssetAtPath<CatalogoPiezas>(RutaCatalogoPiezas);
        if (!c) { c = ScriptableObject.CreateInstance<CatalogoPiezas>(); AssetDatabase.CreateAsset(c, RutaCatalogoPiezas); }
        c.maxMuros = 40; c.maxTrampas = 6; c.maxDefensores = 4; c.huesosPorMejora = 2; c.mejoraMaxima = 3;
        c.piezas.Clear();
        CatalogoPiezas.Coste K(Recurso r, int n) => new CatalogoPiezas.Coste { recurso = r, cantidad = n };
        void Pieza(string id, string nombre, CatalogoPiezas.Categoria cat, GameObject prefab, int vida, string unidad, params CatalogoPiezas.Coste[] costes)
            => c.piezas.Add(new CatalogoPiezas.Pieza { id = id, nombre = nombre, categoria = cat, prefab = prefab, vida = vida, requiereUnidad = unidad, costes = costes.ToList() });
        Pieza("muro_madera", "Muro de madera", CatalogoPiezas.Categoria.Muro, muroMadera, 5, "", K(Recurso.Madera, 2));
        Pieza("muro_piedra", "Muro de piedra", CatalogoPiezas.Categoria.Muro, muroPiedra, 10, "", K(Recurso.Piedra, 3));
        Pieza("puerta", "Puerta", CatalogoPiezas.Categoria.Muro, puerta, 6, "", K(Recurso.Madera, 2), K(Recurso.Hierro, 1));
        Pieza("trampa", "Trampa", CatalogoPiezas.Categoria.Trampa, Cargar("Trampa"), 0, "", K(Recurso.Hierro, 2));
        Pieza("orco_defensor", "Orco defensor", CatalogoPiezas.Categoria.Defensor, Cargar("Orco"), 3, "", K(Recurso.Moneda, 10));
        Pieza("esqueleto_defensor", "Esqueleto defensor", CatalogoPiezas.Categoria.Defensor, Cargar("Esqueleto"), 4, "", K(Recurso.Moneda, 15));
        Pieza("orco_jefe", "Orco jefe", CatalogoPiezas.Categoria.Defensor, Cargar("Jefe"), 15, Jefe.TipoUnidad);
        EditorUtility.SetDirty(c);
    }

    static void CrearTrampa()
    {
        var ctrl = CrearControlador(Dungeon + "trap.fbx", Anim + "trampa.controller");
        var g = new GameObject("Trampa");
        var m = Poner(Dungeon + "trap.fbx", g.transform, Vector3.zero, 0f);
        var an = Animador(m);
        an.runtimeAnimatorController = ctrl;
        an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        g.AddComponent<Trampa>().animador = an;
        Guardar(g, "Trampa");
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
        txt.fontSize = 36; txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        var sombra = hudGo.AddComponent<Shadow>(); sombra.effectDistance = new Vector2(2f, -2f);
        hudGo.AddComponent<HUD>().texto = txt;
        canvasGo.AddComponent<Menu>().icono = AssetDatabase.LoadAssetAtPath<Sprite>(Controles + "icon_menu.png");
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
