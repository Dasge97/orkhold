using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// Cosas del pueblo que no se mueven pero cambian (tarea 7.4): árboles y rocas. Están en todas las copias
/// de la escena; el servidor decide los golpes y avisa por número de árbol (ordenados por posición).
[RequireComponent(typeof(NetworkObject))]
public class MundoRed : NetworkBehaviour
{
    List<FuenteRecurso> _fuentes = new List<FuenteRecurso>();

    public override void OnNetworkSpawn()
    {
        _fuentes = FindObjectsByType<FuenteRecurso>()
            .OrderBy(f => Mathf.Round(f.transform.position.x * 10f)).ThenBy(f => Mathf.Round(f.transform.position.z * 10f)).ToList();
        if (!IsServer) return;
        FuenteRecurso.AlGolpear = (f, desde) => { int i = _fuentes.IndexOf(f); if (i >= 0) GolpeRpc(i, desde); };
        FuenteRecurso.AlAgotar = f => { int i = _fuentes.IndexOf(f); if (i >= 0) AgotarRpc(i); };
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) { FuenteRecurso.AlGolpear = null; FuenteRecurso.AlAgotar = null; }
    }

    [Rpc(SendTo.NotServer)]
    void GolpeRpc(int i, Vector3 desde) { if (i < _fuentes.Count && _fuentes[i]) _fuentes[i].Efecto(desde); }

    [Rpc(SendTo.NotServer)]
    void AgotarRpc(int i) { if (i < _fuentes.Count && _fuentes[i]) _fuentes[i].AgotarDesdeRed(); }
}
