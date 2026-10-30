using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Crea o rehace Assets/Mapas/PaletaMapa.asset con las letras de 02-arquitectura.md §2.2,
/// los modelos de Kenney y los prefabs de Assets/Prefabs. Menú Prototipo > Crear paleta de mapas.
public static class CrearPaletaMapa
{
    public const string Ruta = "Assets/Mapas/PaletaMapa.asset";
    const string Dungeon = CrearPrefabs.Dungeon;
    const string Natura = CrearPrefabs.Natura;

    [MenuItem("Prototipo/Crear paleta de mapas")]
    public static PaletaMapa Crear()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        var p = AssetDatabase.LoadAssetAtPath<PaletaMapa>(Ruta);
        if (!p)
        {
            p = ScriptableObject.CreateInstance<PaletaMapa>();
            AssetDatabase.CreateAsset(p, Ruta);
        }
        if (!CrearPrefabs.Cargar("Puerta")) CrearPrefabs.Crear();

        p.sueloExterior = Modelos(Natura, "ground_grass");
        p.sueloInterior = Modelos(Dungeon, "floor", "floor", "floor", "floor", "floor-detail");
        p.flores = Modelos(Natura, "grass_large", "grass", "grass_leafs", "plant_bushSmall", "plant_bush", "rock_smallA", "rock_smallC",
            "rock_smallFlatA", "mushroom_tanGroup", "grass_leafsLarge");
        p.densidadFlores = 0.2f;   // más relleno: menos suelo vacío
        p.entradas.Clear();
        PaletaMapa.Entrada e;

        // Caminos: sustituyen a la hierba.
        Agregar(p, '=', "camino vertical", Modelos(Natura, "ground_pathStraight"), conSuelo: false);
        Agregar(p, '-', "camino horizontal", Modelos(Natura, "ground_pathStraight"), conSuelo: false, giroY: 90f);
        Agregar(p, '+', "cruce", Modelos(Natura, "ground_pathCross"), conSuelo: false);

        // Río: la pieza (recta, curva, te, cruce, final) se elige según los vecinos. No se puede cruzar.
        e = Agregar(p, '~', "río", new GameObject[0], conSuelo: false, colision: PaletaMapa.Colision.Caja, tamano: Vector3.one);
        e.unirColision = true;
        e.union = new PaletaMapa.PiezasUnion
        {
            recto = Uno(Natura, "ground_riverStraight"), curva = Uno(Natura, "ground_riverCorner"),
            final = Uno(Natura, "ground_riverEndClosed"), te = Uno(Natura, "ground_riverSplit"),
            cruce = Uno(Natura, "ground_riverCross"), aislado = Uno(Natura, "ground_riverTile"), unidaCon = "_",
        };
        // Puente: río debajo y puente de madera encima, girado para cruzarlo. Se puede pasar.
        e = Agregar(p, '_', "puente", Modelos(Natura, "bridge_wood"), conSuelo: false);
        e.debajo = '~';

        // Obstáculos.
        e = Agregar(p, '#', "muro", Modelos(Dungeon, "wall"), colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1.1f, 1f));
        e.unirColision = true;
        e = Agregar(p, 'T', "árbol", Modelos(Natura, "tree_default_dark", "tree_pineRoundA", "tree_pineTallB", "tree_oak_dark", "tree_fat_darkh", "tree_thin_dark"),
                giroAleatorio: true, colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.3f, 2f, 0f));
        e.combinar = false;
        e.combinarEnBorde = 3;
        e.esFuente = true; e.recursoFuente = Recurso.Madera;
        e = Agregar(p, 'r', "roca", Modelos(Natura, "rock_largeA", "rock_largeB", "rock_tallA"),
                giroAleatorio: true, colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.35f, 2f, 0f));
        e.combinar = false;
        e.esFuente = true; e.recursoFuente = Recurso.Piedra;
        Agregar(p, 'f', "flor", Modelos(Natura, "flower_redA", "flower_yellowA", "flower_purpleA", "grass_large"), giroAleatorio: true);
        Agregar(p, 'h', "casa 2x2", new[] { CrearPrefabs.Cargar("Casa") }, bloque: 2,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(2f, 2f, 2f));
        // Valla: tramos de medio metro del centro a cada vecino (valla o entrada). Más alta que el modelo original.
        e = Agregar(p, 'v', "valla", new GameObject[0], colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1f, 1f));
        e.union = new PaletaMapa.PiezasUnion
        {
            brazo = Uno(Natura, "fence_simple"), unidaCon = "E",
            brazoPosicion = new Vector3(-0.47f * 1f, 0f, 0.25f), brazoGiro = new Vector3(0f, 90f, 0f), brazoEscala = new Vector3(0.5f, 1.8f, 1f),
        };
        e.unirColision = true;
        // Casilla reservada (hueco del fuerte en el pueblo): sin suelo ni relleno; encima va el fuerte.
        Agregar(p, ',', "reservada", new GameObject[0], conSuelo: false);

        // Puntos con lógica propia.
        var puerta = CrearPrefabs.Cargar("Puerta");
        Agregar(p, 'F', "la Brasa (aparición)", new GameObject[0], marca: true, entidad: CrearPrefabs.Cargar("Fogata"),
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.35f, 0.5f, 0f));
        Agregar(p, 'C', "cofre PvP", Modelos(Dungeon, "chest"), entidad: puerta, escala: 1.4f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.4f, 0.6f, 0f));
        Agregar(p, 'B', "tablón", Modelos(Natura, "sign"), escala: 2.5f, entidad: CrearPrefabs.Cargar("Tablon"),
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.25f, 1f, 0f));
        Agregar(p, 'D', "puerta de mazmorra", Modelos(Dungeon, "gate"), entidad: puerta);
        Agregar(p, 'E', "salida o entrada", new GameObject[0], entidad: puerta);
        Agregar(p, '[', "esquina noroeste del hueco del fuerte", new GameObject[0], conSuelo: false, marca: true);
        Agregar(p, ']', "esquina sureste del hueco del fuerte", new GameObject[0], conSuelo: false, marca: true);
        Agregar(p, 'o', "orco", new GameObject[0], entidad: CrearPrefabs.Cargar("Orco"));
        Agregar(p, 'a', "arquero", new GameObject[0], entidad: CrearPrefabs.Cargar("Arquero"));
        Agregar(p, 'e', "esqueleto", new GameObject[0], entidad: CrearPrefabs.Cargar("Esqueleto"));
        Agregar(p, 'J', "jefe", new GameObject[0], entidad: CrearPrefabs.Cargar("Jefe"));
        Agregar(p, '$', "moneda", new GameObject[0], entidad: CrearPrefabs.Cargar("Moneda"));
        Agregar(p, 'p', "poción", new GameObject[0], entidad: CrearPrefabs.Cargar("Pocion"));
        Agregar(p, 'X', "trampa", new GameObject[0], entidad: CrearPrefabs.Cargar("Trampa"));
        // Pueblo de Vado Gris (15-interfaz.md §10.2): muralla, torres, calles, farolas, colonos y ruinas.
        Agregar(p, 'm', "muralla", Modelos(CrearPrefabs.Castillo, "wall-narrow-wood"), escala: 1.02f,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1.4f, 1f)).unirColision = true;
        Agregar(p, 'A', "torre de la muralla", Modelos(CrearPrefabs.Castillo, "tower-hexagon-base"), escala: 1.35f,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1.8f, 1f));
        Agregar(p, 's', "calle empedrada", Modelos(Dungeon, "floor", "floor", "floor-detail"), conSuelo: false, giroY: 0f);
        Agregar(p, 'l', "farola", Modelos(CrearPrefabs.Cementerio, "lightpost-single"), escala: 1.5f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.12f, 1.8f, 0f));
        Agregar(p, 'i', "tienda de colono", Modelos(Natura, "tent_detailedOpen", "tent_smallClosed"), escala: 2.2f, giroAleatorio: true,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1.2f, 1f));
        Agregar(p, 'k', "leña", Modelos(Natura, "log_stackLarge", "log_stack"), escala: 1.6f, giroAleatorio: true,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(0.9f, 0.6f, 0.9f));
        Agregar(p, 'g', "barriles", Modelos(Dungeon, "barrel"), escala: 0.9f, giroAleatorio: true,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.3f, 0.7f, 0f));
        Agregar(p, 't', "estandarte", Modelos(CrearPrefabs.Castillo, "flag-banner-long"), escala: 1.2f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.1f, 2f, 0f));
        Agregar(p, 'x', "columna rota", Modelos(Natura, "statue_columnDamaged", "statue_column"), escala: 2.2f, giroAleatorio: true,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.3f, 1.4f, 0f));
        Agregar(p, 'R', "torre en ruinas", Modelos(Dungeon, "wall-half", "column", "wall-narrow"), escala: 1.1f, giroAleatorio: true,
                colision: PaletaMapa.Colision.Caja, tamano: new Vector3(1f, 1.5f, 1f));
        Agregar(p, 'z', "mesa", Modelos(Dungeon, "table"), escala: 1.1f, colision: PaletaMapa.Colision.Caja, tamano: new Vector3(0.9f, 0.4f, 0.7f));
        // Mazmorra 2, la Forja Hundida (11-mazmorras.md §2).
        Agregar(p, 'H', "orco herrero", new GameObject[0], entidad: CrearPrefabs.Cargar("OrcoHerrero"));
        Agregar(p, 'Q', "bombardero", new GameObject[0], entidad: CrearPrefabs.Cargar("Bombardero"));
        Agregar(p, 'Z', "trampa de fuego", new GameObject[0], entidad: CrearPrefabs.Cargar("TrampaFuego"));
        // Decoración de la Forja con el Graveyard Kit (14.1).
        Agregar(p, 'b', "brasero", new GameObject[0], entidad: CrearPrefabs.Cargar("Brasero"),
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.3f, 0.7f, 0f));
        Agregar(p, 'c', "columna", Modelos(CrearPrefabs.Cementerio, "column-large"), escala: 1.6f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.35f, 1.5f, 0f));
        Agregar(p, 'u', "urna", Modelos(CrearPrefabs.Cementerio, "urn-round", "urn-square"), giroAleatorio: true, escala: 1.6f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.25f, 0.5f, 0f));
        Agregar(p, 'd', "escombros", Modelos(CrearPrefabs.Cementerio, "debris", "debris-wood"), giroAleatorio: true, escala: 1.4f);
        Agregar(p, 'N', "Grimbra la Fundidora", new GameObject[0], entidad: CrearPrefabs.Cargar("Grimbra"));
        // Puertas de las mazmorras 2, 3 y 4 en el pueblo (se abren al vencer al jefe anterior).
        Agregar(p, 'Y', "puerta de la mazmorra 2", Modelos(Dungeon, "gate"), entidad: puerta);
        Agregar(p, 'U', "puerta de la mazmorra 3", Modelos(Dungeon, "gate"), entidad: puerta);
        Agregar(p, 'W', "puerta de la mazmorra 4", Modelos(Dungeon, "gate"), entidad: puerta);
        // Carteles del tutorial (tarea 9.2 y 07-lore.md §7): el texto va en la cabecera del mapa (cartel1 ... cartel5)
        // y lo dice un personaje del pueblo que pone MontarEscenas. 'G': Gruk, el orco mercenario.
        for (char n = '1'; n <= '5'; n++)
            Agregar(p, n, "personaje del tutorial " + n, new GameObject[0], marca: true);
        Agregar(p, 'G', "Gruk", new GameObject[0], marca: true);
        Agregar(p, 'K', "cofre del tesoro", Modelos(Dungeon, "chest"), escala: 1.6f, marca: true,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.45f, 0.7f, 0f));

        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();
        Debug.Log($"Paleta de mapas guardada en {Ruta} con {p.entradas.Count} letras");
        return p;
    }

    static GameObject[] Modelos(string carpeta, params string[] nombres)
    {
        var r = nombres.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>(carpeta + n + ".fbx")).ToArray();
        for (int i = 0; i < r.Length; i++) if (!r[i]) Debug.LogWarning("No encuentro " + carpeta + nombres[i] + ".fbx");
        return r.Where(m => m).ToArray();
    }

    static GameObject Uno(string carpeta, string nombre) => Modelos(carpeta, nombre).FirstOrDefault();

    static PaletaMapa.Entrada Agregar(PaletaMapa p, char letra, string descripcion, GameObject[] modelos,
                        bool conSuelo = true, float giroY = 0f, bool giroAleatorio = false, float escala = 1f,
                        PaletaMapa.Colision colision = PaletaMapa.Colision.Ninguna, Vector3 tamano = default,
                        int bloque = 1, bool marca = false, GameObject entidad = null)
    {
        var e = new PaletaMapa.Entrada
        {
            letra = letra, descripcion = descripcion, modelos = modelos.Where(m => m).ToArray(), conSuelo = conSuelo,
            giroY = giroY, giroAleatorio = giroAleatorio, escala = escala, colision = colision,
            tamano = tamano == default ? Vector3.one : tamano, bloque = bloque, marca = marca, entidad = entidad,
        };
        p.entradas.Add(e);
        return e;
    }
}
