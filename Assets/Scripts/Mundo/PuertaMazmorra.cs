using UnityEngine;

/// Puerta de las mazmorras 2-4 en el pueblo (11-mazmorras.md §1): solo se abre si ya has vencido al jefe
/// de la mazmorra anterior. Si no, Ansel avisa.
[RequireComponent(typeof(Puerta))]
public class PuertaMazmorra : MonoBehaviour
{
    public int numero = 2;

    /// Se puede entrar: el jefe de la mazmorra anterior está vencido.
    public bool Abierta => numero <= 1 || Partida.Actual.Progreso($"jefe{numero - 1}_muerto") == "1";
    /// La mazmorra ya existe en el juego (las que aún no están construidas tienen la puerta, pero no la escena).
    public bool Existe => Application.CanStreamedLevelBeLoaded(GetComponent<Puerta>().destino);

    void Start()
    {
        GetComponent<Puerta>().antesDeIr = () =>
        {
            if (!Existe) { Avisos.Mostrar("Ansel: \"Ahí hay una entrada, pero todavía no sé adónde lleva.\"", 3f); return false; }
            if (Abierta) return true;
            Avisos.Mostrar("Ansel: \"Todavía no he encontrado la entrada. Vence antes al jefe de la mazmorra " + (numero - 1) + ".\"", 3f);
            return false;
        };
    }
}
