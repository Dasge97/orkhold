using System;

/// La partida en curso. Vive entre escenas (es estática); la fase 4 la guarda en disco.
public static class Partida
{
    static DatosPartida _actual = new DatosPartida();
    static DatosInventario _alEntrar;   // inventario al entrar en la mazmorra

    public static DatosPartida Actual => _actual;
    public static DatosInventario Inventario => _actual.inventario;

    /// Cualquier cambio de inventario, bajas o progreso.
    public static event Action AlCambiar;

    public static void Cambiado() => AlCambiar?.Invoke();

    public static void Sumar(Recurso r, int n = 1) { Inventario[r] += n; Cambiado(); }

    /// Quita "n" si hay bastante; devuelve si se pudo.
    public static bool Gastar(Recurso r, int n = 1)
    {
        if (Inventario[r] < n) return false;
        Inventario[r] -= n; Cambiado(); return true;
    }

    public static void Reemplazar(DatosPartida datos) { _actual = datos ?? new DatosPartida(); Cambiado(); }

    /// Se llama al entrar en la mazmorra: lo recogido desde aquí es el botín de esta entrada.
    public static void EmpezarEntrada() { _alEntrar = Inventario.Copia(); }

    /// Al morir en la mazmorra: se pierde la mitad (redondeando a favor del jugador) de lo ganado en esta entrada.
    /// Devuelve cuántas unidades se perdieron en total.
    public static int PerderMitadDeLaEntrada()
    {
        if (_alEntrar == null) return 0;
        int perdido = 0;
        foreach (var r in DatosInventario.Todos)
        {
            int ganado = Inventario[r] - _alEntrar[r];
            if (ganado <= 0) continue;
            int quitar = ganado / 2;
            Inventario[r] -= quitar;
            perdido += quitar;
        }
        _alEntrar = null;
        Cambiado();
        return perdido;
    }
}
