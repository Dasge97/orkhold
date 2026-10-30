using UnityEngine;

/// En el cofre del saqueo del pueblo (puerta "asalto"): abre "Preparar saqueo" (08-formato-y-pvp.md §4.1).
/// El saqueo lo decide el servidor: sin conexión no se puede saquear.
[RequireComponent(typeof(Puerta))]
public class BuscarRival : MonoBehaviour
{
    void Start()
    {
        GetComponent<Puerta>().antesDeIr = () =>
        {
            if (!Sesion.Conectado) Avisos.Mostrar("Sin conexión: el saqueo necesita el servidor", 2.5f);
            else PantallaPrepararSaqueo.Abrir();
            return false;
        };
    }
}
