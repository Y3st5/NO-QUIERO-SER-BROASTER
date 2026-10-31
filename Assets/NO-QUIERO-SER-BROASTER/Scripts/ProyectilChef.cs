using UnityEngine;

/// <summary>
/// Proyectil que lanza el Chef siguiendo una curva parabólica (Rigidbody).
/// - Si toca a la Paloma: le quita una vida y desaparece.
/// - Si toca el suelo (tag "Suelo"): deja un charco y desaparece.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ProyectilChef : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private int danio = 1;

    [Header("Charco")]
    [Tooltip("Prefab del charco que se crea al tocar el suelo.")]
    [SerializeField] private GameObject charcoPrefab;

    [Tooltip("Solo se crea charco si la superficie es suficientemente horizontal.")]
    [Range(0f, 1f)]
    [SerializeField] private float inclinacionMaximaSuelo = 0.7f;

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
        if (cuerpo == null)
            cuerpo = GetComponent<Rigidbody>();

        if (propioCollider == null)
            propioCollider = GetComponent<Collider>();

        if (cuerpo != null)
            cuerpo.linearVelocity = velocidad;

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

        // 1) ¿Le dio a la Paloma?
        Paloma vida = colision.collider.GetComponentInParent<Paloma>();
        if (vida != null)
        {
            vida.RecibirDanio(danio);
            Destroy(gameObject);
            return;
        }

        // 2) ¿Cayó al suelo?
        if (colision.collider.CompareTag("Suelo") && colision.contactCount > 0)
        {
            ContactPoint contacto = colision.GetContact(0);

            if (contacto.normal.y >= inclinacionMaximaSuelo)
                CrearCharco(contacto);

            Destroy(gameObject);
        }
    }

    private void CrearCharco(ContactPoint contacto)
    {
        if (charcoPrefab == null)
            return;

        Vector3 posicion = contacto.point + contacto.normal * 0.05f;
        Quaternion rotacion = Quaternion.FromToRotation(Vector3.up, contacto.normal);
        Instantiate(charcoPrefab, posicion, rotacion);
    }
}
