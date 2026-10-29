using UnityEngine;

/// Círculo rojo en el suelo que avisa de un golpe en área. Se llena durante el aviso y desaparece.
public class AvisoArea : MonoBehaviour
{
    static Mesh _disco;
    static Material _material;

    MeshRenderer _r;
    Transform _relleno;
    float _duracion, _t;

    public static AvisoArea Crear(Vector3 centro, float radio, float duracion)
    {
        if (!_disco) _disco = Disco(40);
        if (!_material)
            _material = new Material(Shader.Find("Sprites/Default")) { color = new Color(1f, 0.1f, 0.05f, 0.35f), name = "Aviso area" };
        var go = new GameObject("AvisoArea");
        go.transform.position = centro + Vector3.up * 0.03f;
        go.transform.localScale = new Vector3(radio, 1f, radio);
        go.AddComponent<MeshFilter>().sharedMesh = _disco;
        var a = go.AddComponent<AvisoArea>();
        a._r = go.AddComponent<MeshRenderer>();
        a._r.sharedMaterial = _material;
        a._duracion = duracion;
        // Un segundo disco que crece desde el centro: cuando llena el círculo, golpea.
        var relleno = new GameObject("Relleno");
        relleno.transform.SetParent(go.transform, false);
        relleno.transform.localPosition = Vector3.up * 0.005f;
        relleno.AddComponent<MeshFilter>().sharedMesh = _disco;
        relleno.AddComponent<MeshRenderer>().sharedMaterial = _material;
        relleno.transform.localScale = Vector3.zero;
        a._relleno = relleno.transform;
        return a;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / _duracion);
        _relleno.localScale = new Vector3(k, 1f, k);
        if (_t >= _duracion + 0.15f) Destroy(gameObject);
    }

    /// Disco plano de radio 1 en el plano XZ.
    static Mesh Disco(int lados)
    {
        var m = new Mesh { name = "Disco" };
        var v = new Vector3[lados + 1];
        var t = new int[lados * 3];
        v[0] = Vector3.zero;
        for (int i = 0; i < lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            v[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % lados + 1; t[i * 3 + 2] = i + 1;
        }
        m.vertices = v; m.triangles = t;
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
