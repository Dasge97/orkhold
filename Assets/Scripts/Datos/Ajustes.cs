using UnityEngine;

/// Ajustes del jugador (sonido, vibración). Van en PlayerPrefs, aparte de la partida:
/// reiniciar la partida no los toca.
public static class Ajustes
{
    public static bool Sonido
    {
        get => PlayerPrefs.GetInt("ajuste_sonido", 1) == 1;
        set { PlayerPrefs.SetInt("ajuste_sonido", value ? 1 : 0); PlayerPrefs.Save(); Sonidos.Activo = value; }
    }

    public static bool Vibracion
    {
        get => PlayerPrefs.GetInt("ajuste_vibracion", 1) == 1;
        set { PlayerPrefs.SetInt("ajuste_vibracion", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar() { Sonidos.Activo = Sonido; }

    /// Vibración corta (al recibir daño), si está activada.
    public static void Vibrar()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Vibracion && !Application.isEditor) Handheld.Vibrate();
#endif
    }
}
