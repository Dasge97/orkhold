using UnityEngine;

/// Personaje del tutorial (tarea 9.2, 15-interfaz.md §5): su texto ya no salta al acercarse. Lleva su nombre encima,
/// una exclamación mientras no hayas hablado con él, y con el botón de interactuar ("Hablar") abre la caja de diálogo.
/// Los textos vienen de la cabecera del mapa (cartel1 ... cartel5 en pueblo.txt, "Nombre: texto"); los coloca MontarEscenas.
public class Cartel : Interactuable
{
    [TextArea] public string texto = "";
    public float radio = 2.4f;

    /// Oficio de cada personaje (07-lore.md §5), debajo de su nombre.
    public static string Oficio(string quien) =>
        quien == "Maela" ? "Guardiana de la Brasa" : quien == "Tobin" ? "Tablonero" : quien == "Ferra" ? "Herrera"
        : quien == "Ansel" ? "Cartógrafo" : quien == "Rook" ? "Vigilante del Este" : quien == "Gruk" ? "Mercenario"
        : quien == "Urzog" ? "Guardián de la Cripta" : "";

    /// Texto que se ve ahora en la caja de diálogo si lo dice un cartel; vacío si no (para los tests).
    public static string Visible => Dialogo.Abierto && _hablando ? _hablando.Frase : "";

    static Cartel _hablando;
    Rotulo _rotulo;

    /// "Maela: texto" → "Maela".
    public string Nombre { get { int i = texto.IndexOf(':'); return i > 0 && i < 24 ? texto.Substring(0, i).Trim() : ""; } }
    /// "Maela: texto" → "texto".
    public string Frase { get { int i = texto.IndexOf(':'); return i > 0 && i < 24 ? texto.Substring(i + 1).Trim() : texto; } }

    public override Icono IconoBoton => Icono.Hablar;
    public override string Verbo => "Hablar";
    string ClaveHablado => "hablado-" + Nombre;

    void Start()
    {
        alcance = Mathf.Max(alcance, 1.8f);
        _rotulo = Rotulo.Poner(gameObject, Rotulo.Tipo.Personaje, Nombre, Oficio(Nombre));
        _rotulo.novedad = Partida.Actual.Progreso(ClaveHablado) != "1";
    }

    public override void Usar(ControlJugador jugador)
    {
        _hablando = this;
        if (_rotulo) _rotulo.novedad = false;
        if (Partida.Actual.Progreso(ClaveHablado) != "1") Partida.Actual.PonerProgreso(ClaveHablado, "1");
        Mirar(jugador);
        var herreria = GetComponent<Herreria>();
        if (herreria) Dialogo.Abrir(transform, Nombre, Oficio(Nombre), Nombre, Frase, ("Fabricar", () => herreria.Usar(jugador)));
        else if (Nombre == "Tobin") Dialogo.Abrir(transform, Nombre, Oficio(Nombre), Nombre, Frase, ("Clasificación", AbrirRanking));
        else Dialogo.Abrir(transform, Nombre, Oficio(Nombre), Nombre, Frase);
    }

    static void AbrirRanking()
    {
        if (!Sesion.Conectado) { Avisos.Mostrar("Tobin: \"Sin servidor no hay tablón.\"", 2.5f); return; }
        PantallaRanking.Abrir();
    }

    /// El personaje se gira hacia quien le habla.
    void Mirar(ControlJugador jugador)
    {
        if (!jugador) return;
        Vector3 d = jugador.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_hablando == this) _hablando = null;
    }
}
