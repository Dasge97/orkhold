using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Utilidades comunes de los tests en Play Mode.
public static class Ayudas
{
    /// Carga una escena del juego directamente (sin transición) y espera a que esté lista.
    public static IEnumerator Cargar(string escena)
    {
        Viaje.llegarA = "";
        SceneManager.LoadScene(escena);
        yield return null;
        yield return null;
        Assert.AreEqual(escena, SceneManager.GetActiveScene().name);
    }

    /// El jugador de este móvil (en el pueblo con red puede haber más copias).
    public static ControlJugador Jugador()
    {
        var todos = Object.FindObjectsByType<ControlJugador>();
        if (todos.Length > 1)
            Debug.LogWarning("PRUEBA: hay " + todos.Length + " jugadores: " + string.Join(", ", todos.Select(j =>
                $"{j.name}@{j.transform.position} escena={j.gameObject.scene.name} local={j == ControlJugador.Local}")));
        return ControlJugador.Local ? ControlJugador.Local : (todos.Length > 0 ? todos[0] : null);
    }

    public static Puerta Puerta(string id)
    {
        var todas = Object.FindObjectsByType<Puerta>().Where(x => x.id == id).ToArray();
        if (todas.Length > 1)
            Debug.LogWarning("PRUEBA: hay " + todas.Length + " puertas " + id + ": " + string.Join(", ", todas.Select(x =>
                $"{x.name}@{x.transform.position} llegada={x.llegada} escena={x.gameObject.scene.name}")));
        var p = todas.FirstOrDefault();
        Assert.IsNotNull(p, "No hay puerta con id " + id + " en " + SceneManager.GetActiveScene().name);
        return p;
    }

    /// Lleva al jugador lejos de la puerta (para armarla) y luego encima.
    public static IEnumerator Pisar(Puerta p)
    {
        var j = Jugador().transform;
        Vector3 fuera = p.llegada - p.transform.position; fuera.y = 0f;
        if (fuera.sqrMagnitude < 0.01f) fuera = Vector3.back;
        GestorEscena.Colocar(j, p.transform.position + fuera.normalized * 2.5f, 0f);
        yield return null;
        yield return null;
        GestorEscena.Colocar(j, p.transform.position, 0f);
        yield return null;
    }

    /// Espera a que la escena activa sea la indicada y la transición haya terminado.
    public static IEnumerator EsperarEscena(string escena, float limite = 10f)
    {
        float t = 0f;
        while ((SceneManager.GetActiveScene().name != escena || Transicion.Ocupada) && t < limite)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Assert.AreEqual(escena, SceneManager.GetActiveScene().name, "No se llegó a la escena a tiempo");
        Assert.IsFalse(Transicion.Ocupada, "La transición no terminó");
    }

    /// Foto de lo que ve la cámara del juego (sin la interfaz) en Revision/<nombre>.png.
    public static void Foto(string nombre)
    {
        var cam = Camera.main;
        if (!cam) return;
        System.IO.Directory.CreateDirectory("Revision");
        var rt = new RenderTexture(1280, 720, 24);
        var antes = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = antes;
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes($"Revision/{nombre}.png", tex.EncodeToPNG());
        Object.Destroy(tex); Object.Destroy(rt);
    }

    public static float DistanciaPlana(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }
}
