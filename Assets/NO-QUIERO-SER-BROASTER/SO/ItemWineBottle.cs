using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "NuevaBotellaVino", menuName = "Inventario/Items/Botella Vino")]
public class ItemWineBottle : Item
{
    [Title("Configuración de la Botella de Vino")]
    public bool drinkable = true;

    public override bool Usar(GameObject usuario)
    {
        if (drinkable)
        {
            Debug.Log($"El usuario {usuario.name} ha bebido de la botella '{nombre}'.");
            // Lógica de curación aquí...
            return true; // Se consumió
        }
        else
        {
            Debug.Log($"La botella '{nombre}' está vacía o no es bebible.");
            return false; // No se consumió
        }
    }
}