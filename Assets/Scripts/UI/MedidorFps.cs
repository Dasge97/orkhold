using UnityEngine;
using UnityEngine.SceneManagement;

/// Fija 60 fps como objetivo (Android limita a 30 por defecto) y escribe en el registro
/// la media de fps cada 5 s, con la escena. En el móvil se lee con "adb logcat -s Unity".
public class MedidorFps : MonoBehaviour
{
    public const float Periodo = 5f;
    public static float UltimaMedia { get; private set; }

    int _frames;
    float _tiempo;
    float _peor = float.MaxValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        var go = new GameObject("MedidorFps");
        DontDestroyOnLoad(go);
        go.AddComponent<MedidorFps>();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _frames++;
        _tiempo += dt;
        if (dt > 0f) _peor = Mathf.Min(_peor, 1f / dt);
        if (_tiempo < Periodo) return;
        UltimaMedia = _frames / _tiempo;
        Debug.Log($"FPS media={UltimaMedia:F1} peor={_peor:F1} escena={SceneManager.GetActiveScene().name}");
        _frames = 0; _tiempo = 0f; _peor = float.MaxValue;
    }
}
