using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Prueba de CargadorMapa: carga el mapa de ejemplo de 02-arquitectura.md §2.2 y uno interior
/// en una escena temporal (no se guarda), comprueba cabecera, marcas y bloques, y hace fotos.
/// Deja Revision/cargador.txt y Revision/20-cargador-*.png.
/// Menú Prototipo > Probar cargador de mapas, o en batch con -executeMethod PruebaCargadorMapa.EnBatch.
public static class PruebaCargadorMapa
{
    const string Carpeta = "Revision";

    const string Exterior =
@"nombre: pueblo
tipo: exterior
aparicion: F
---
TTTTTTTTTTTTTTTTTTTT
T........=.........T
T..r.....=....o....T
T........=.........T
T...F....+----B....T
T........=....C....T
T........=.........T
T........D....[]...T
TTTTTTTTTTTTTTTTTTTT
";

    const string Interior =
@"nombre: prueba-interior
tipo: mazmorra
---
#######
#..hh.#
#..hh.#
#.$.X.#
###E###
";

    const string Rio =
@"nombre: prueba-rio
tipo: exterior
---
..~....
..~....
..~~~..
....~..
..--_--
....~..
";

    [MenuItem("Prototipo/Probar cargador de mapas")]
    public static bool Probar()
    {
        Directory.CreateDirectory(Carpeta);
        var paleta = CrearPaletaMapa.Crear();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var sb = new StringBuilder();
        bool ok = true;
        void Comprobar(bool cond, string que) { sb.AppendLine((cond ? "OK    " : "FALLO ") + que); ok &= cond; }

        // --- Exterior ---
        var mapaA = CargadorMapa.Leer(Exterior);
        Comprobar(mapaA.Nombre == "pueblo" && mapaA.Valor("tipo") == "exterior" && mapaA.Valor("aparicion") == "F", "cabecera leída");
        Comprobar(mapaA.Ancho == 20 && mapaA.Alto == 9, $"tamaño 20x9 (sale {mapaA.Ancho}x{mapaA.Alto})");
        var a = CargadorMapa.Colocar(mapaA, paleta, null, Instanciar);
        string marcasA = new string(a.marcas.Select(m => m.letra).ToArray());
        Comprobar(marcasA == "oFBCD[]", $"marcas en orden de lectura (sale '{marcasA}')");
        Comprobar(a.letrasDesconocidas.Count == 0, "sin letras desconocidas");
        var fogata = a.marcas.First(m => m.letra == 'F');
        Comprobar(fogata.posicion == new Vector3(4f, 0f, 4f), $"fogata en (4,0,4) (sale {fogata.posicion})");
        int arboles = a.raiz.transform.Find("Piezas").Cast<Transform>().Count(t => t.GetComponent<CapsuleCollider>() && t.name.StartsWith("tree"));
        Comprobar(arboles == 2 * 20 + 2 * 7, $"54 árboles con cápsula (sale {arboles})");
        sb.AppendLine($"exterior: modelos={a.modelos}");

        // --- Interior, desplazado para que no se solape ---
        var mapaB = CargadorMapa.Leer(Interior);
        var padreB = new GameObject("Interior");
        padreB.transform.position = new Vector3(30f, 0f, 0f);
        var b = CargadorMapa.Colocar(mapaB, paleta, padreB.transform, Instanciar);
        var piezasB = b.raiz.transform.Find("Piezas").Cast<Transform>().ToList();
        var casas = piezasB.Where(t => t.name.StartsWith("Casa")).ToList();
        Comprobar(casas.Count == 1, $"casa 2x2 puesta una sola vez (sale {casas.Count})");
        if (casas.Count == 1) Comprobar(casas[0].localPosition == new Vector3(3.5f, 0f, 2.5f), $"casa centrada en (3.5,0,2.5) (sale {casas[0].localPosition})");
        int muros = piezasB.Count(t => t.name.StartsWith("wall"));
        Comprobar(muros == 7 + 2 * 3 + 6, $"19 muros (sale {muros})");
        Comprobar(b.colisionesUnidas == 1 + 2 * 3 + 2, $"muros: 9 cajas, una por tramo de fila (sale {b.colisionesUnidas})");
        string marcasB = new string(b.marcas.Select(m => m.letra).ToArray());
        Comprobar(marcasB == "$XE", $"marcas interior (sale '{marcasB}')");
        var salida = b.marcas.First(m => m.letra == 'E');
        Comprobar(salida.posicion == new Vector3(33f, 0f, 0f), $"marca en coordenadas del mundo (sale {salida.posicion})");
        bool sueloInterior = b.raiz.transform.Find("Suelo").Cast<Transform>().All(t => t.name.StartsWith("floor"));
        Comprobar(sueloInterior, "suelo de mazmorra en el mapa interior");
        sb.AppendLine($"interior: modelos={b.modelos}");

        // --- Río con curvas y puente ---
        var padreC = new GameObject("Rio");
        padreC.transform.position = new Vector3(-15f, 0f, 0f);
        var cr = CargadorMapa.Cargar(Rio, paleta, padreC.transform, Instanciar);
        var todas = cr.raiz.GetComponentsInChildren<Transform>().Where(t => t.parent && (t.parent.name == "Piezas" || t.parent.name == "Suelo")).ToList();
        string En(Vector3 local, string prefijo)
        {
            var t = todas.FirstOrDefault(x => x.localPosition == local && x.name.StartsWith(prefijo));
            return t ? $"{t.name}@{Mathf.RoundToInt(t.localEulerAngles.y) % 360}" : "nada";
        }
        void Pieza(Vector3 local, string esperado)
        {
            string sale = En(local, esperado.Split('@')[0].Substring(0, 6));
            Comprobar(sale == esperado, $"río en {local}: {esperado} (sale {sale})");
        }
        Pieza(new Vector3(2f, 0f, 5f), "ground_riverStraight@0");
        Pieza(new Vector3(2f, 0f, 3f), "ground_riverCorner@270");
        Pieza(new Vector3(4f, 0f, 3f), "ground_riverCorner@90");
        Pieza(new Vector3(4f, 0f, 1f), "ground_riverStraight@0");
        Pieza(new Vector3(4f, 0f, 1f), "bridge_wood@0");
        Pieza(new Vector3(4f, 0f, 0f), "ground_riverStraight@0");
        Comprobar(cr.colisionesUnidas == 5, $"río: 5 cajas, una por tramo de fila (sale {cr.colisionesUnidas})");
        bool puenteLibre = todas.Where(t => t.localPosition == new Vector3(4f, 0f, 1f)).All(t => !t.GetComponent<Collider>());
        Comprobar(puenteLibre, "el puente se puede cruzar");

        // --- Valla con entrada ---
        var padreD = new GameObject("Valla");
        padreD.transform.position = new Vector3(-15f, 0f, 12f);
        var dv = CargadorMapa.Cargar(@"nombre: prueba-valla
tipo: fuerte
---
vvvv
E..v
vvvv
", paleta, padreD.transform, Instanciar);
        var esquina = dv.raiz.transform.Find("Piezas").GetChild(0);
        Comprobar(esquina.childCount == 2, $"esquina de valla con 2 tramos (sale {esquina.childCount})");
        Comprobar(dv.entidades.Count == 1 && dv.entidades[0].GetComponent<Puerta>(), "la entrada E pone una Puerta");

        Foto("23-cargador-valla", new Vector3(-13.5f, 5f, 9.5f), new Vector3(-13.5f, 0f, 13f), 45f);
        Foto("22-cargador-rio", new Vector3(-11.5f, 9f, -4f), new Vector3(-11.5f, 0f, 2.5f), 45f);
        Foto("20-cargador-exterior", new Vector3(9.5f, 16f, -6f), new Vector3(9.5f, 0f, 4f), 50f);
        Foto("21-cargador-interior", new Vector3(33f, 8f, -3f), new Vector3(33f, 0f, 2f), 45f);

        sb.Insert(0, (ok ? "RESULTADO: OK" : "RESULTADO: FALLO") + "\n");
        File.WriteAllText(Carpeta + "/cargador.txt", sb.ToString());
        Debug.Log("Prueba del cargador de mapas:\n" + sb);
        return ok;
    }

    /// Para la línea de órdenes: sale con 0 si todo va bien y 1 si algo falla.
    public static void EnBatch()
    {
        bool ok = false;
        try { ok = Probar(); }
        catch (System.Exception ex) { Debug.LogException(ex); }
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static GameObject Instanciar(GameObject m) => (GameObject)PrefabUtility.InstantiatePrefab(m);

    static void Foto(string nombre, Vector3 desde, Vector3 hacia, float fov)
    {
        var go = new GameObject("CamaraFoto");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        cam.fieldOfView = fov;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.72f, 0.85f, 0.95f);
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes($"{Carpeta}/{nombre}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
    }
}
