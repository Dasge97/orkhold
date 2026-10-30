using Unity.Netcode;
using UnityEngine;

/// Moneda, poción o material del pueblo online: lo recoge el servidor, que lo quita para todos.
[RequireComponent(typeof(Recogible), typeof(NetworkObject))]
public class RecogibleRed : NetworkBehaviour
{
    readonly NetworkVariable<int> _cantidad = new NetworkVariable<int>(1);

    public override void OnNetworkSpawn()
    {
        var r = GetComponent<Recogible>();
        if (IsServer)
        {
            _cantidad.Value = r.cantidad;
            r.alUsar = () => { if (IsSpawned) NetworkObject.Despawn(true); };
        }
        else r.cantidad = _cantidad.Value;
    }
}
