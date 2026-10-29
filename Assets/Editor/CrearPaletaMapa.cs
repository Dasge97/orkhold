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
        p.flores = Modelos(Natura, "flower_redA", "flower_yellowA", "flower_purpleA", "grass_large", "grass", "plant_bushSmall", "mushroom_redGroup");
        p.densidadFlores = 0.06f;
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
        e = Agregar(p, 'T', "árbol", Modelos(Natura, "tree_default", "tree_pineRoundA", "tree_pineRoundB", "tree_oak", "tree_fat"),
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
        Agregar(p, 'F', "fogata (aparición)", Modelos(Natura, "campfire_stones"), marca: true, entidad: CrearPrefabs.Cargar("Fogata"),
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.35f, 0.5f, 0f));
        Agregar(p, 'C', "cofre PvP", Modelos(Dungeon, "chest"), entidad: puerta, escala: 1.4f,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.4f, 0.6f, 0f));
        Agregar(p, 'B', "tablón", Modelos(Natura, "sign"), escala: 2.5f, marca: true,
                colision: PaletaMapa.Colision.Capsula, tamano: new Vector3(0.25f, 1f, 0f));
        Agregar(p, 'D', "puerta de mazmorra", Modelos(Dungeon, "gate"), entidad: puerta);
        Agregar(p, 'E', "salida o entrada", new GameObject[0], entidad: puerta);
        Agregar(p, '[', "esquina noroeste del hueco del fuerte", new GameObject[0], conSuelo: false, marca: true);
        Agregar(p, ']', "esquina sureste del hueco del fuerte", new GameObject[0], conSuelo: false, marca: true);
        Agregar(p, 'o', "orco", new GameObject[0], entidad: CrearPrefabs.Cargar("Orco"));
        Agregar(p, 'a', "arquero", new GameObject[0], marca: true);
        Agregar(p, 'e', "esqueleto", new GameObject[0], marca: true);
        Agregar(p, 'J', "jefe", new GameObject[0], marca: true);
        Agregar(p, '$', "moneda", new GameObject[0], entidad: CrearPrefabs.Cargar("Moneda"));
        Agregar(p, 'p', "poción", new GameObject[0], entidad: CrearPrefabs.Cargar("Pocion"));
        Agregar(p, 'X', "trampa", Modelos(Dungeon, "trap"), marca: true);
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
