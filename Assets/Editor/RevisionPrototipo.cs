using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Revisión automática: para cada escena del juego saca una foto con la vista de juego y otra del
/// mapa entero, y escribe un informe con lo que hay. Fuera de batch, entra además en Play en el
/// pueblo para capturar la interfaz andando y atacando.
/// Se dispara sola si existe Revision/pedir.txt; también desde Prototipo > Revisar, o en batch con
/// -executeMethod RevisionPrototipo.EnBatch.
[InitializeOnLoad]
public static class RevisionPrototipo
{
    public const string Carpeta = "Revision";
    const string ClavePlay = "RevisionPrototipo.play";

    static RevisionPrototipo()
    {
        int intentos = 0;
        void Intentar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(Carpeta + "/pedir.txt")) return;
            if (!File.Exists(MontarEscenas.CarpetaEscenas + "Pueblo.unity") || EditorApplication.isCompiling || EditorApplication.isUpdating)
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
        Fotos();
        if (Application.isBatchMode) return;
        EditorSceneManager.OpenScene(MontarEscenas.CarpetaEscenas + "Pueblo.unity");
        EditorPrefs.SetBool(ClavePlay, true);
        EditorApplication.EnterPlaymode();
    }

    public static void EnBatch()
    {
        try { Fotos(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    /// Fotos en modo edición de cada escena e informe.
    public static void Fotos()
    {
        Directory.CreateDirectory(Carpeta);
        var sb = new StringBuilder();
        int n = 0;
        foreach (var escena in MontarEscenas.Escenas)
        {
            EditorSceneManager.OpenScene(MontarEscenas.CarpetaEscenas + escena + ".unity");
            var jugador = Object.FindAnyObjectByType<ControlJugador>();
            var mapa = GameObject.Find("Mapa " + NombreMapa(escena)) ?? Object.FindObjectsByType<Transform>().FirstOrDefault(t => t.name.StartsWith("Mapa "))?.gameObject;
            var b = Limites(mapa);
            var puertas = Object.FindObjectsByType<Puerta>();
            sb.AppendLine($"== {escena}: objetos={Object.FindObjectsByType<Transform>().Length} renderers={Object.FindObjectsByType<Renderer>().Length} " +
                          $"colliders={Object.FindObjectsByType<Collider>().Length} enemigos={Object.FindObjectsByType<Enemigo>().Length} " +
                          $"recogibles={Object.FindObjectsByType<Recogible>().Length} mapa={b.size}");
            foreach (var p in puertas)
                sb.AppendLine($"   puerta id={p.id} destino={p.destino} llegarA={p.llegarA} alPisar={p.alPisar} en={p.transform.position} llegada={p.llegada}");
            if (jugador)
            {
                Vector3 p = jugador.transform.position + Vector3.up * 0.4f;
                sb.AppendLine($"   jugador en {jugador.transform.position}");
                Foto($"{++n:00}-{escena}-vista-juego", p + new Vector3(0f, 5.5f, -6.5f), p, 38f, Color.black);
            }
            float lado = Mathf.Max(b.size.x, b.size.z);
            Foto($"{++n:00}-{escena}-mapa", b.center + new Vector3(0f, lado * 0.95f, -lado * 0.55f), b.center, 55f, Color.black);
        }
        File.WriteAllText(Carpeta + "/informe.txt", sb.ToString());
        Debug.Log("Revisión: informe y fotos en " + Carpeta + "\n" + sb);
    }

    static string NombreMapa(string escena) => escena == "Pueblo" ? "pueblo" : escena == "Mazmorra1" ? "catacumbas" : escena == "Mazmorra2" ? "forja" : "fuerte-base";

    static Bounds Limites(GameObject g)
    {
        if (!g) return new Bounds(Vector3.zero, Vector3.one * 20f);
        var rs = g.GetComponentsInChildren<Renderer>().Where(r => r.bounds.size.x < 40f).ToArray();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.one * 20f);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static void Foto(string nombre, Vector3 desde, Vector3 hacia, float fov, Color fondo)
    {
        var go = new GameObject("CamaraFoto");
        var cam = go.AddComponent<Camera>();
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        cam.fieldOfView = fov;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = RenderSettings.fogColor;
        bool niebla = RenderSettings.fog;
        RenderSettings.fog = false;   // la niebla tapa el mapa entero visto desde lejos
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderSettings.fog = niebla;
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
        new GameObject("RevisionEnPlay").AddComponent<RevisionEnPlay>();
    }

    class RevisionEnPlay : MonoBehaviour
    {
        System.Collections.IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Carpeta + "/50-play-quieto.png");
            yield return new WaitForSeconds(0.5f);
            float t = 0f;
            while (t < 1.5f) { EntradaJuego.MoverPantalla = new Vector2(0f, 1f); t += Time.deltaTime; yield return null; }
            ScreenCapture.CaptureScreenshot(Carpeta + "/51-play-andando.png");
            yield return new WaitForSeconds(0.5f);
            EntradaJuego.MoverPantalla = Vector2.zero;
            yield return null;
            EntradaJuego.AtaquePantalla = true;
            yield return new WaitForSeconds(0.25f);
            ScreenCapture.CaptureScreenshot(Carpeta + "/52-play-atacando.png");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("Revisión en Play terminada");
            EditorApplication.ExitPlaymode();
        }
    }
}
