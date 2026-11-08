using UnityEngine;
using Sirenix.OdinInspector;

// Exigimos Rigidbody y Collider para que la física funcione sí o sí
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BotellaFisica : MonoBehaviour
{
    [Title("Efectos de Rotura")]
    [Required] // Evita que el objeto se rompa sin instanciar nada (evita el NullReference)
    [Tooltip("El prefab con los cristales rotos o el sistema de partículas a instanciar al romperse")]
    public GameObject prefabBotellaRota;

    [PropertyRange(0f, 20f)] // Crea un slider para ajustar la sensibilidad de rotura fácilmente
    [Tooltip("Velocidad mínima de impacto para que se rompa (evita que se rompa al apoyarla suavemente)")]
    public float fuerzaMinimaRotura = 2f;

    [Title("Configuración de Capas")]
    [Required]
    [Tooltip("La capa que se considera como superficie rompible (ej: Floor)")]
    public LayerMask capaSuelo;

    /// <summary>
    /// Detecta la colisión física real.
    /// </summary>
    private void OnCollisionEnter(Collision colision)
    {
        // Verificamos si el objeto chocado pertenece a la LayerMask configurada
        // Usamos el operador bitwise & para comprobar la capa de forma eficiente
        if (((1 << colision.gameObject.layer) & capaSuelo) != 0)
        {
            // Verificamos con qué fuerza chocó para no romperla si solo rodó por el suelo
            if (colision.relativeVelocity.magnitude >= fuerzaMinimaRotura)
            {
                Romper();
            }
        }
    }

    [Button(ButtonSizes.Medium)] // Permite probar la rotura desde el editor sin necesidad de jugar
    private void Romper()
    {
        // 1. Instanciamos los cristales rotos o partículas en la misma posición y rotación
        if (prefabBotellaRota != null)
        {
            Instantiate(prefabBotellaRota, transform.position, transform.rotation);
        }

        // 2. Destruimos la botella entera
        Destroy(gameObject);
    }
}