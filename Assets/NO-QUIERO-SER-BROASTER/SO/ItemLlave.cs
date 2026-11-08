using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NuevaLlave", menuName = "Inventario/Items/Llave")]
public class ItemLlave : Item
{
    [Title("Configuración de la Llave")]
    [Required]
    [Tooltip("Código único que debe coincidir con el código de la puerta")]
    [LabelText("Código de Cerradura")]
    public string codigoDeCerradura;

    [ToggleLeft]
    [Tooltip("¿La llave se destruye después de abrir la puerta?")]
    public bool seConsumeAlUsar = true;

    /// <summary>
    /// Lógica al intentar "consumir" la llave directamente desde la UI del inventario.
    /// </summary>
    public override bool Usar(GameObject usuario)
    {
        // Una llave normalmente no hace nada si se le da doble clic en el inventario.
        // Debe usarse interactuando directamente con la puerta.
        Debug.Log($"No puedes usar la llave '{nombre}' en el aire. Interactúa con una cerradura.");

        // Devolvemos false porque la llave NO se ha gastado/consumido en esta acción.
        return false;
    }
}