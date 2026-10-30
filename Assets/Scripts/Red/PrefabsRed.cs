using UnityEngine;

/// Prefabs de red del pueblo online (con NetworkObject). Vive en Assets/Resources/PrefabsRed.asset;
/// lo rellena CrearPrefabs.
[CreateAssetMenu(menuName = "Orkhold/Prefabs de red", fileName = "PrefabsRed")]
public class PrefabsRed : ScriptableObject
{
    public GameObject jugador;
    public GameObject orco;
    public GameObject mundo;
    [Tooltip("Un recogible por recurso, en el orden de Recurso: moneda, madera, piedra, hierro, hueso, poción.")]
    public GameObject[] recogibles = new GameObject[0];

    static PrefabsRed _instancia;
    public static PrefabsRed Instancia => _instancia ? _instancia : (_instancia = Resources.Load<PrefabsRed>("PrefabsRed"));

    public GameObject Recogible(Recurso r) => (int)r < recogibles.Length ? recogibles[(int)r] : null;

    public System.Collections.Generic.IEnumerable<GameObject> Todos()
    {
        if (jugador) yield return jugador;
        if (orco) yield return orco;
        if (mundo) yield return mundo;
        foreach (var r in recogibles) if (r) yield return r;
    }
}
