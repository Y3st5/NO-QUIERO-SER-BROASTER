using UnityEngine;

/// <summary>
/// Proyectil que lanza a la Paloma por los aires al impactar (knockback).
/// No hace daño ni deja charco: solo interrumpe el control del jugador.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProyectilLanzamiento : MonoBehaviour
{
    [Header("Empuje")]
    [Tooltip("Fuerza vertical (hacia arriba) que recibe la Paloma.")]
    [SerializeField] private float fuerzaVertical = 8f;

    [Tooltip("Fuerza horizontal (alejándola del impacto) que recibe la Paloma.")]
    [SerializeField] private float fuerzaHorizontal = 5f;

    [Tooltip("Segundos que la Paloma queda sin control.")]
    [SerializeField] private float duracionSinControl = 1f;

    [Header("Vida")]
    [Tooltip("Segundos antes de autodestruirse si no impacta en nada.")]
    [SerializeField] private float vidaMaxima = 6f;

    private Rigidbody cuerpo;
    private Collider propioCollider;
    private bool lanzado;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        propioCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Aplica la velocidad calculada por el Chef e ignora las colisiones con quien lo lanzó.
    /// </summary>
    public void Inicializar(Transform lanzador, Vector3 velocidad)
    {
        if (cuerpo == null) cuerpo = GetComponent<Rigidbody>();
        if (propioCollider == null) propioCollider = GetComponent<Collider>();

        if (cuerpo != null) cuerpo.linearVelocity = velocidad;

        if (lanzador != null && propioCollider != null)
        {
            Collider[] collidersLanzador = lanzador.GetComponentsInChildren<Collider>();
            for (int i = 0; i < collidersLanzador.Length; i++)
                Physics.IgnoreCollision(propioCollider, collidersLanzador[i], true);
        }

        lanzado = true;
        Destroy(gameObject, Mathf.Max(0.5f, vidaMaxima));
    }

    private void OnCollisionEnter(Collision colision)
    {
        if (!lanzado)
            return;

        inputsPaloma jugador = colision.collider.GetComponentInParent<inputsPaloma>();
        if (jugador != null)
        {
            // Dirección del empuje: la del vuelo del proyectil (solo horizontal).
            Vector3 direccion = cuerpo != null ? cuerpo.linearVelocity : Vector3.zero;
            direccion.y = 0f;

            if (direccion.sqrMagnitude < 0.01f)
            {
                direccion = jugador.transform.position - transform.position;
                direccion.y = 0f;
            }

            direccion = direccion.sqrMagnitude > 0.01f ? direccion.normalized : -jugador.transform.forward;

            jugador.LanzarAlAire(direccion * fuerzaHorizontal + Vector3.up * fuerzaVertical, duracionSinControl);
            Destroy(gameObject);
            return;
        }

        // Cualquier otra superficie: desaparece sin dejar nada.
        if (colision.contactCount > 0)
            Destroy(gameObject);
    }
}
