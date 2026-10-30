using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// Enemigo del pueblo online (tarea 7.4): solo piensa en el servidor; los clientes reciben su posición
/// (NetworkTransform), su animación, su vida y si se ve.
[RequireComponent(typeof(Enemigo), typeof(NetworkObject))]
public class EnemigoRed : NetworkBehaviour
{
    readonly NetworkVariable<FixedString32Bytes> _anim = new NetworkVariable<FixedString32Bytes>();
    readonly NetworkVariable<int> _vida = new NetworkVariable<int>();
    readonly NetworkVariable<bool> _visible = new NetworkVariable<bool>(true);

    Enemigo _e;
    Vida _v;

    void Awake()
    {
        _e = GetComponent<Enemigo>();
        _v = GetComponent<Vida>();
    }

    public override void OnNetworkSpawn()
    {
        var control = GetComponent<CharacterController>();
        if (control && control.enabled) { control.enabled = false; control.enabled = true; }   // ver JugadorRed
        if (IsServer)
        {
            _vida.Value = _v.actual;
            _v.AlCambiar += () => _vida.Value = _v.actual;
            return;
        }
        // En los clientes no piensa ni choca: solo se dibuja.
        _e.enabled = false;
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        _v.Establecer(_vida.Value, transform.position);
        _vida.OnValueChanged += (_, n) => _v.Establecer(n, transform.position + transform.forward);
        _anim.OnValueChanged += (_, n) => _e.AnimarDesdeRed(n.ToString());
        _visible.OnValueChanged += (_, n) => _e.MostrarDesdeRed(n);
        _e.MostrarDesdeRed(_visible.Value);
        if (_anim.Value.Length > 0) _e.AnimarDesdeRed(_anim.Value.ToString());
    }

    void Update()
    {
        if (!IsSpawned || !IsServer) return;
        var a = _e.EstadoAnim ?? "";
        if (_anim.Value.ToString() != a) _anim.Value = new FixedString32Bytes(a);
        if (_visible.Value != _e.Visible) _visible.Value = _e.Visible;
    }
}
