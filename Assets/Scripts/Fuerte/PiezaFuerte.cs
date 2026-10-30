using System;
using System.Collections;
using UnityEngine;

/// Una pieza del fuerte en escena (01-diseno.md §5.1). Según el modo:
/// - Propio (escena Fuerte): la puerta se abre sola al acercarse el dueño; los defensores están quietos.
/// - Asalto (saqueo): muros y puertas se rompen a golpes (cada golpe, 1 de vida); trampas y defensores, quietos.
/// - Vista (hueco del fuerte en el pueblo): solo se ve.
public class PiezaFuerte : MonoBehaviour, IGolpeable
{
    public enum Modo { Propio, Asalto, Vista }

    public string tipo;
    public CatalogoPiezas.Categoria categoria;
    public Modo modo;
    public int x, z;
    public Vida vida;
    [Tooltip("Modelo que tiembla al golpearlo y gira al abrirse (puerta).")]
    public Transform modelo;
    public bool esPuerta;

    /// Muro o puerta rota en el saqueo.
    public static event Action<PiezaFuerte> AlRomper;
    /// Cada golpe que recibe un muro o una puerta en el saqueo (el servidor rehace la exploración con ellos).
    public static event Action<PiezaFuerte> AlGolpear;

    ControlJugador _jugador;
    Collider _col;
    bool _abierta;
    Quaternion _giroModelo;

    public bool PuedeRecibirGolpe => modo == Modo.Asalto && categoria == CatalogoPiezas.Categoria.Muro && vida && !vida.Muerto;
    public Transform Transform => transform;
    public bool Abierta => _abierta;

    void Start()
    {
        _col = GetComponent<Collider>();
        if (modelo) _giroModelo = modelo.localRotation;
    }

    void Update()
    {
        if (!esPuerta || modo != Modo.Propio) return;
        if (!_jugador) { _jugador = FindAnyObjectByType<ControlJugador>(); if (!_jugador) return; }
        Vector3 d = _jugador.transform.position - transform.position; d.y = 0f;
        bool abrir = d.magnitude < 1.3f;
        if (abrir == _abierta) return;
        _abierta = abrir;
        if (_col) _col.enabled = !abrir;
        if (modelo) modelo.localRotation = abrir ? _giroModelo * Quaternion.Euler(0f, 90f, 0f) : _giroModelo;
        if (abrir) Sonidos.Tocar("puerta", transform.position, 0.6f);
    }

    public void RecibirGolpe(int dano, Vector3 desde, ControlJugador quien)
    {
        if (!PuedeRecibirGolpe) return;
        AlGolpear?.Invoke(this);
        vida.Danar(1, desde);
        Sonidos.Tocar(tipo == "muro_piedra" ? "golpe_roca" : "golpe_madera", transform.position);
        if (vida.Muerto) { Romper(); return; }
        StopAllCoroutines();
        StartCoroutine(Temblar());
    }

    void Romper()
    {
        Sonidos.Tocar("romper_muro", transform.position);
        AlRomper?.Invoke(this);
        Destroy(gameObject);
    }

    IEnumerator Temblar()
    {
        if (!modelo) yield break;
        Vector3 p0 = modelo.localPosition;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            modelo.localPosition = p0 + new Vector3(Mathf.Sin(t * 90f) * 0.04f, 0f, 0f);
            yield return null;
        }
        modelo.localPosition = p0;
    }
}
