using UnityEngine;

/// Aro dorado en el suelo bajo lo que vas a usar con el botón de interactuar (15-interfaz.md §6). Late despacio.
/// Un segundo aro marca a qué vas a pegar al atacar: rojo si es un enemigo, blanco si es un árbol o una roca.
/// La textura se dibuja en código; el material es la copia de Resources/Particula (se ve sin luz y con transparencia).
public class AroInteraccion : MonoBehaviour
{
    static AroInteraccion _instancia, _objetivoAro;
    Transform _objetivo;
    MeshRenderer _render;
    float _tam = 1f;

    /// Pone el aro bajo "objetivo"; null lo esconde.
    public static void Seguir(Interactuable objetivo)
    {
        if (!_instancia)
        {
            if (!objetivo) return;
            _instancia = Crear("Aro interaccion", new Color(1f, 0.7f, 0.25f, 0.6f));
        }
        _instancia._objetivo = objetivo ? objetivo.transform : null;
        _instancia._render.enabled = objetivo;
        if (objetivo) _instancia._tam = Mathf.Clamp(objetivo.alcance * 0.9f, 0.9f, 1.8f);
    }

    static readonly Color Rojo = new Color(1f, 0.15f, 0.08f, 0.85f), Blanco = new Color(0.8f, 0.8f, 0.75f, 0.45f);

    /// Pone el aro del objetivo de ataque bajo "objetivo" (rojo si es enemigo); null lo esconde.
    public static void MarcarObjetivo(Transform objetivo, bool enemigo)
    {
        if (!_objetivoAro)
        {
            if (!objetivo) return;
            _objetivoAro = Crear("Aro objetivo", Rojo);
        }
        _objetivoAro._objetivo = objetivo;
        _objetivoAro._render.enabled = objetivo;
        if (!objetivo) return;
        _objetivoAro._tam = enemigo ? 1.15f : 1.5f;
        _objetivoAro._render.sharedMaterial.SetColor("_TintColor", enemigo ? Rojo : Blanco);
    }

    /// Lo que marca ahora el aro del objetivo (para los tests); null si no hay.
    public static Transform ObjetivoMarcado => _objetivoAro && _objetivoAro._render.enabled ? _objetivoAro._objetivo : null;

    static AroInteraccion Crear(string nombre, Color tinte)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = nombre;
        Destroy(go.GetComponent<Collider>());
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var a = go.AddComponent<AroInteraccion>();
        a._render = go.GetComponent<MeshRenderer>();
        a._render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        a._render.receiveShadows = false;
        var baseMat = Resources.Load<Material>("ParticulaSuma");
        var mat = baseMat ? new Material(baseMat) : new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = Textura();
        mat.color = new Color(1f, 0.75f, 0.3f, 0.8f);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", tinte);
        a._render.sharedMaterial = mat;
        return a;
    }

    /// Anillo suave de 128 px: transparente en el centro y fuera.
    static Texture2D Textura()
    {
        const int n = 128;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                px[y * n + x] = new Color(1f, 1f, 1f, a * a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    void LateUpdate()
    {
        if (!_objetivo) { if (_render.enabled) _render.enabled = false; return; }
        transform.position = new Vector3(_objetivo.position.x, _objetivo.position.y + 0.04f, _objetivo.position.z);
        transform.Rotate(0f, 0f, 30f * Time.deltaTime, Space.Self);
        transform.localScale = Vector3.one * _tam * (1f + 0.06f * Mathf.Sin(Time.time * 4f));
    }
}
