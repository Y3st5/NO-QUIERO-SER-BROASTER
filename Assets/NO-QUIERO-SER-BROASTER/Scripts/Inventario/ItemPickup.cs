using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Puente entre un objeto del mundo y un ítem del inventario (ScriptableObject).
/// Implementa <see cref="IInteractable"/> para que la Paloma pueda recogerlo con la tecla E.
/// </summary>
[DisallowMultipleComponent]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Title("Ítem")]
    [Required]
    [Tooltip("ScriptableObject del ítem que se guardará en el inventario.")]
    [SerializeField] private Item item;

    [MinValue(1)]
    [Tooltip("Cuántas unidades se añaden al inventario al recogerlo.")]
    [SerializeField] private int cantidad = 1;

    [Title("Comportamiento")]
    [Tooltip("Si está activo, el objeto desaparece de la escena al recogerse.")]
    [SerializeField] private bool destruirAlRecoger = true;

    [Tooltip("Efecto opcional que se instancia en la posición del objeto al recogerlo.")]
    [SerializeField] private GameObject prefabEfectoRecogida;

    [Title("Depuración")]
    [SerializeField] private bool mostrarLogs = true;

    public Item Item => item;
    public int Cantidad => cantidad;

    public string Prompt
    {
        get
        {
            if (item == null)
                return "Recoger";

            string nombreItem = string.IsNullOrEmpty(item.nombre) ? item.name : item.nombre;
            return cantidad > 1 ? $"Recoger {nombreItem} x{cantidad}" : $"Recoger {nombreItem}";
        }
    }

    public void Interact(inputsPaloma player)
    {
        if (item == null)
        {
            Debug.LogWarning($"[ItemPickup] '{name}' no tiene un ítem asignado.", this);
            return;
        }

        Inventario inventario = BuscarInventario(player);
        if (inventario == null)
        {
            Debug.LogWarning("[ItemPickup] No se encontró ningún Inventario en el jugador.", this);
            return;
        }

        if (!inventario.AnadirItem(item, cantidad))
        {
            if (mostrarLogs)
                Debug.Log($"[ItemPickup] Inventario lleno, no se pudo recoger '{item.name}'.", this);
            return;
        }

        if (mostrarLogs)
            Debug.Log($"[ItemPickup] Recogido '{item.name}' x{cantidad}.", this);

        if (prefabEfectoRecogida != null)
            Instantiate(prefabEfectoRecogida, transform.position, transform.rotation);

        if (destruirAlRecoger)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private static Inventario BuscarInventario(inputsPaloma player)
    {
        if (player != null)
        {
            Inventario inventario = player.GetComponentInParent<Inventario>();
            if (inventario == null)
                inventario = player.GetComponentInChildren<Inventario>(true);
            if (inventario != null)
                return inventario;
        }

        // Respaldo: buscamos el inventario del objeto con tag "Player".
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            Inventario inventario = jugador.GetComponentInChildren<Inventario>(true);
            if (inventario != null)
                return inventario;
        }

        return FindAnyObjectByType<Inventario>();
    }

    /// <summary>Botón para probar la recogida directamente desde el inspector en el editor.</summary>
    [Button("Probar recogida")]
    private void ProbarRecogida()
    {
        inputsPaloma jugador = FindAnyObjectByType<inputsPaloma>();
        Interact(jugador);
    }
}
