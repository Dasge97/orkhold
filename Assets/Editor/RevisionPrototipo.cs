using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Revisión automática: saca fotos de la escena y del personaje desde varios ángulos, escribe un
/// informe con materiales y clips, y entra en Play para capturar la interfaz andando y atacando.
/// Se dispara sola si existe el fichero Revision/pedir.txt; también desde Prototipo > Revisar.
[InitializeOnLoad]
public static class RevisionPrototipo
{
    const string Carpeta = "Revision";
    const string Escena = "Assets/Scenes/PvE.unity";
    const string ClavePlay = "RevisionPrototipo.play";

    static RevisionPrototipo()
    {
        int intentos = 0;
        void Intentar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(Carpeta + "/pedir.txt")) return;
            // Si la escena aún se está montando, se vuelve a intentar un poco después.
            if (!File.Exists(Escena) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (++intentos < 60) EditorApplication.delayCall += Intentar;
                return;
            }
            File.Delete(Carpeta + "/pedir.txt");
            Revisar();
        }
        EditorApplication.delayCall += Intentar;
    }

    [MenuItem("Prototipo/Revisar (fotos + informe)")]
    public static void Revisar()
    {
        Directory.CreateDirectory(Carpeta);
        if (EditorSceneManager.GetActiveScene().path != Escena) EditorSceneManager.OpenScene(Escena);

        var sb = new StringBuilder();
        var jugador = GameObject.Find("Jugador");
        var modelo = jugador ? jugador.transform.Find("Modelo") : null;

        // Materiales del personaje y de un par de piezas del mundo.
        foreach (var nombre in new[] { "Jugador/Modelo", "Mundo/Mazmorra", "Mundo/Suelo" })
        {
            var g = GameObject.Find(nombre);
            if (!g) { sb.AppendLine("NO EXISTE " + nombre); continue; }
            var r = g.GetComponentsInChildren<Renderer>().FirstOrDefault();
            if (!r) { sb.AppendLine(nombre + ": sin renderer"); continue; }
            foreach (var m in r.sharedMaterials)
                sb.AppendLine($"{nombre}: material={m?.name} shader={m?.shader?.name} textura={(m && m.mainTexture ? m.mainTexture.name : "NINGUNA")} color={(m ? m.color.ToString() : "-")}");
        }

        // Clips y bucles.
        var imp = AssetImporter.GetAtPath("Assets/Kenney/MiniDungeon/character-human.fbx") as ModelImporter;
        if (imp)
        {
            sb.AppendLine("rig=" + imp.animationType);
            foreach (var c in imp.clipAnimations.Where(c => new[] { "idle", "walk", "sprint", "attack-melee-right", "die" }.Contains(c.name)))
                sb.AppendLine($"clip {c.name}: bucle={c.loopTime}");
        }
        if (modelo)
        {
            var an = modelo.GetComponent<Animator>();
            sb.AppendLine($"Animator: {(an ? "sí" : "NO")} controlador={(an && an.runtimeAnimatorController ? an.runtimeAnimatorController.name : "NINGUNO")}");
            var b = modelo.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            sb.AppendLine($"Modelo: tamaño={b.size} centro={b.center} rot={modelo.rotation.eulerAngles}");
        }
        sb.AppendLine($"Escena: objetos={Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Length} enemigos={Object.FindObjectsByType<Enemigo>(FindObjectsSortMode.None).Length} recogibles={Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).Length}");

        // Fotos en modo edición.
        if (jugador)
        {
            Vector3 p = jugador.transform.position + Vector3.up * 0.4f;
            Foto("01-vista-juego", p + new Vector3(0f, 7f, -6f), p, 40f);
            Foto("02-personaje-desde-delante(+Z)", p + new Vector3(0f, 0.3f, 2f), p, 30f);
            Foto("03-personaje-desde-detras(-Z)", p + new Vector3(0f, 0.3f, -2f), p, 30f);
            Foto("04-personaje-desde-derecha(+X)", p + new Vector3(2f, 0.3f, 0f), p, 30f);
            Foto("05-mazmorra", new Vector3(7f, 9f, 2f), new Vector3(7f, 0f, 7f), 45f);
            Foto("06-mapa-entero", new Vector3(0f, 26f, -8f), Vector3.zero, 60f);
        }
        File.WriteAllText(Carpeta + "/informe.txt", sb.ToString());
        Debug.Log("Revisión: informe y fotos en " + Carpeta);

        // Parte en Play: capturas con interfaz, andando y atacando.
        EditorPrefs.SetBool(ClavePlay, true);
        EditorApplication.EnterPlaymode();
    }

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

    /// En Play: espera, anda, ataca, y hace capturas de la vista Game con la interfaz.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AlEntrarEnPlay()
    {
        if (!EditorPrefs.GetBool(ClavePlay, false)) return;
        EditorPrefs.SetBool(ClavePlay, false);
        var go = new GameObject("RevisionEnPlay");
        go.AddComponent<RevisionEnPlay>();
    }

    class RevisionEnPlay : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Carpeta + "/10-play-quieto.png");
            yield return new WaitForSeconds(0.5f);
            float t = 0f;
            while (t < 1.5f) { EntradaJuego.MoverPantalla = new Vector2(0f, 1f); t += Time.deltaTime; yield return null; }
            ScreenCapture.CaptureScreenshot(Carpeta + "/11-play-andando.png");
            yield return new WaitForSeconds(0.5f);
            EntradaJuego.MoverPantalla = Vector2.zero;
            yield return null;
            EntradaJuego.AtaquePantalla = true;
            yield return new WaitForSeconds(0.25f);
            ScreenCapture.CaptureScreenshot(Carpeta + "/12-play-atacando.png");
            yield return new WaitForSeconds(0.5f);
            var j = FindFirstObjectByType<ControlJugador>();
            File.AppendAllText(Carpeta + "/informe.txt", $"\nPLAY: jugador en {j?.transform.position} rot={j?.transform.rotation.eulerAngles} estado anim={(j && j.animador ? j.animador.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name : "?")}\n");
            Debug.Log("Revisión en Play terminada");
            EditorApplication.ExitPlaymode();
        }
    }
}
