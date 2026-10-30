using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// Texto arriba a la izquierda con vida y recursos.
public class HUD : MonoBehaviour
{
    public Text texto;
    public Vida vida;
    public Inventario inventario;

    void Start()
    {
        Partida.AlCambiar += Refrescar;
        Enganchar();
        Refrescar();
    }

    /// En el pueblo online el jugador aparece después: se engancha cuando haya uno.
    void Update() { if (!vida) Enganchar(); }

    void Enganchar()
    {
        var j = ControlJugador.Local;
        if (!j) return;
        vida = j.GetComponent<Vida>();
        inventario = j.GetComponent<Inventario>();
        vida.AlCambiar += Refrescar; vida.AlMorir += Refrescar;
        Refrescar();
    }

    void OnDestroy() { Partida.AlCambiar -= Refrescar; }

    void Refrescar()
    {
        if (!texto) return;
        if (vida && vida.Muerto) { texto.text = "Has muerto"; return; }
        var inv = Partida.Inventario;
        var sb = new StringBuilder();
        if (vida) sb.Append($"Vida {vida.actual}/{vida.maxima}");
        sb.Append($"\nMonedas {inv.monedas}   Madera {inv.madera}   Piedra {inv.piedra}   Hierro {inv.hierro}   Hueso {inv.hueso}   Pociones {inv.pociones}");
        texto.text = sb.ToString();
    }
}
