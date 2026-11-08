using UnityEngine;
using Sirenix.OdinInspector;
using System;

public abstract class Item : ScriptableObject
{
    [Title("Identificación")]
    [ReadOnly]
    [Tooltip("ID único generado automáticamente para evitar colisiones.")]
    public string id; // Cambiado a string para usar GUIDs

    [Required]
    [LabelText("Nombre del Ítem")]
    public string nombre;

    [TextArea(3, 10)]
    [LabelText("Descripción Detallada")]
    public string descripcion;

    [Title("Visual")]
    [Required]
    [PreviewField(100, ObjectFieldAlignment.Left)]
    public Sprite icono;

    [Title("Configuración de Inventario")]
    [MinValue(1)]
    public int maxStack = 1;

    [ToggleLeft]
    public bool esConsumible;

    /// <summary>
    /// Devuelve TRUE si el ítem se usó con éxito.
    /// Esto permite al Gestor de Inventario saber si debe restar 1 a la cantidad actual.
    /// </summary>
    [Button(ButtonSizes.Large)]
    public abstract bool Usar(GameObject usuario);

    // Generación automática del ID cuando se crea o modifica el SO en el editor
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
        }
    }
}