using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Representa un espacio individual dentro del inventario.
/// Al ser [Serializable], Unity puede guardarlo y mostrarlo en el inspector.
/// </summary>
[Serializable]
public class SlotInventario
{
    [HideLabel, HorizontalGroup("Slot", 0.5f)]
    public Item item;

    [HideLabel, HorizontalGroup("Slot", 0.5f)]
    [MinValue(0)]
    public int cantidad;

    public bool PuedeApilar(Item itemAComprobar)
    {
        return item != null && item == itemAComprobar && cantidad < item.maxStack;
    }

    public void Vaciar()
    {
        item = null;
        cantidad = 0;
    }
}

/// <summary>
/// El núcleo de datos del inventario. Va colocado directamente en el Player (Paloma).
/// </summary>
public class Inventario : MonoBehaviour
{
    [Title("Configuración")]
    [MinValue(1)]
    public int capacidad = 10;

    [Title("Estado Actual")]
    [TableList(AlwaysExpanded = true, IsReadOnly = true)]
    public List<SlotInventario> slots = new List<SlotInventario>();

    // Evento al que la Interfaz de Usuario (UI) se suscribirá
    public event Action AlCambiarInventario;

    private void Awake()
    {
        // Creamos los slots vacíos al iniciar el juego
        for (int i = 0; i < capacidad; i++)
        {
            slots.Add(new SlotInventario());
        }
    }

    /// <summary>
    /// Intenta añadir un ítem. Devuelve true si hubo espacio, false si el inventario está lleno.
    /// </summary>
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
    public bool AnadirItem(Item nuevoItem, int cantidadAAgregar = 1)
    {
        if (nuevoItem == null || cantidadAAgregar <= 0) return false;

        // 1. Buscamos si ya tenemos este ítem y podemos apilarlo
        foreach (SlotInventario slot in slots)
        {
            if (slot.PuedeApilar(nuevoItem))
            {
                int espacioDisponible = nuevoItem.maxStack - slot.cantidad;
                int cantidadAInsertar = Mathf.Min(cantidadAAgregar, espacioDisponible);

                slot.cantidad += cantidadAInsertar;
                cantidadAAgregar -= cantidadAInsertar;

                if (cantidadAAgregar == 0)
                {
                    AlCambiarInventario?.Invoke();
                    return true;
                }
            }
        }

        // 2. Si sobra cantidad, buscamos slots vacíos
        while (cantidadAAgregar > 0)
        {
            SlotInventario slotVacio = slots.Find(s => s.item == null);
            if (slotVacio != null)
            {
                slotVacio.item = nuevoItem;
                int cantidadAInsertar = Mathf.Min(cantidadAAgregar, nuevoItem.maxStack);

                slotVacio.cantidad = cantidadAInsertar;
                cantidadAAgregar -= cantidadAInsertar;
            }
            else
            {
                AlCambiarInventario?.Invoke();
                return false; // Inventario lleno
            }
        }

        AlCambiarInventario?.Invoke();
        return true;
    }

    /// <summary>
    /// Quita una cantidad específica de un ítem.
    /// </summary>
    public bool QuitarItem(Item itemAQuitar, int cantidadAQuitar = 1)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item == itemAQuitar)
            {
                if (slots[i].cantidad >= cantidadAQuitar)
                {
                    slots[i].cantidad -= cantidadAQuitar;
                    if (slots[i].cantidad == 0) slots[i].Vaciar();

                    AlCambiarInventario?.Invoke();
                    return true;
                }
                else
                {
                    cantidadAQuitar -= slots[i].cantidad;
                    slots[i].Vaciar();
                }
            }
        }

        AlCambiarInventario?.Invoke();
        return cantidadAQuitar == 0;
    }
}