using UnityEngine;
using UnityEngine.UI;

/// Texto arriba a la izquierda con vida, monedas, pociones y bajas.
public class HUD : MonoBehaviour
{
    public Text texto;
    public Vida vida;
    public Inventario inventario;

    void Start()
    {
        var j = FindAnyObjectByType<ControlJugador>();
        if (j)
        {
            if (!vida) vida = j.GetComponent<Vida>();
            if (!inventario) inventario = j.GetComponent<Inventario>();
        }
        if (vida) { vida.AlCambiar += Refrescar; vida.AlMorir += Refrescar; }
        if (inventario) inventario.AlCambiar += Refrescar;
        Refrescar();
    }

    void Refrescar()
    {
        if (!texto) return;
        string vidaTxt = vida ? $"Vida {vida.actual}/{vida.maxima}" : "";
        texto.text = vida && vida.Muerto
            ? "Has muerto. Reiniciando..."
            : $"{vidaTxt}\nMonedas: {inventario?.monedas ?? 0}   Pociones: {inventario?.pociones ?? 0}   Bajas: {inventario?.bajas ?? 0}";
    }
}
