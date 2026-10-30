using UnityEngine;

/// Pone en el jugador el modelo elegido al crear el personaje (Mini Characters).
/// Se ejecuta antes que los demás componentes del jugador para que usen el modelo nuevo.
[DefaultExecutionOrder(-200)]
public class PersonajeJugador : MonoBehaviour
{
    /// Modelo puesto ahora (para los tests).
    public string Actual { get; private set; } = "";

    void Awake() => Poner(Partida.Actual.modelo);

    public void Poner(string id)
    {
        var cat = Catalogo.Instancia;
        if (string.IsNullOrEmpty(id) || !cat || cat.idsPersonajes == null) return;
        int i = System.Array.IndexOf(cat.idsPersonajes, id);
        if (i < 0 || i >= cat.modelosPersonajes.Length || !cat.modelosPersonajes[i]) return;
        var viejo = transform.Find("Modelo");
        if (viejo) DestroyImmediate(viejo.gameObject);
        var m = Instantiate(cat.modelosPersonajes[i], transform);
        m.name = "Modelo";
        m.transform.localPosition = Vector3.zero;
        m.transform.localRotation = Quaternion.identity;
        m.transform.localScale = Vector3.one * cat.escalaPersonajes;
        var an = m.GetComponentInChildren<Animator>();
        if (!an) an = m.AddComponent<Animator>();
        an.applyRootMotion = false;
        an.runtimeAnimatorController = cat.controladoresPersonajes[i];
        var ctrl = GetComponent<ControlJugador>();
        if (ctrl) ctrl.animador = an;
        Actual = id;
    }
}
