using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Representa un espacio individual dentro del inventario.
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
/// Nodo para la lista circular doblemente enlazada del inventario.
/// </summary>
[Serializable]
public class NodoInventario
{
    public SlotInventario slot = new SlotInventario();
    [NonSerialized] public NodoInventario siguiente;
    [NonSerialized] public NodoInventario anterior;
}

/// <summary>
/// El núcleo del inventario implementado como una Lista Circular Doblemente Enlazada.
/// </summary>
public class Inventario : MonoBehaviour
{
    [Title("Configuración")]
    [MinValue(1)]
    public int capacidad = 4;

    [Title("Estado Actual")]
    [ReadOnly]
    [NonSerialized]
    public NodoInventario nodoActual;

    // Evento al que la Interfaz de Usuario (UI) se suscribirá
    public event Action AlCambiarInventario;

    private void Awake()
    {
        AsegurarInicializado();
    }

    /// <summary>
    /// Construye la lista circular si todavía no existe.
    /// Permite usar el inventario también en el editor (donde Awake no se ejecuta)
    /// sin reconstruirla y perder los ítems ya guardados.
    /// </summary>
    public void AsegurarInicializado()
    {
        if (nodoActual == null)
            InicializarListaCircular();
    }

    private void InicializarListaCircular()
    {
        if (capacidad < 1) return;

        NodoInventario primero = null;
        NodoInventario ultimo = null;

        for (int i = 0; i < capacidad; i++)
        {
            NodoInventario nuevoNodo = new NodoInventario();
            if (primero == null)
            {
                primero = nuevoNodo;
                ultimo = nuevoNodo;
            }
            else
            {
                ultimo.siguiente = nuevoNodo;
                nuevoNodo.anterior = ultimo;
                ultimo = nuevoNodo;
            }
        }

        // Cerrar el círculo
        ultimo.siguiente = primero;
        primero.anterior = ultimo;

        nodoActual = primero;
    }

    /// <summary>
    /// Convierte la lista circular en una lista simple para que la UI pueda iterar sobre ella.
    /// </summary>
    public List<SlotInventario> GetSlotsForUI()
    {
        List<SlotInventario> listaUI = new List<SlotInventario>();
        AsegurarInicializado();
        if (nodoActual == null) return listaUI;

        NodoInventario buscador = nodoActual;
        for (int i = 0; i < capacidad; i++)
        {
            listaUI.Add(buscador.slot);
            buscador = buscador.siguiente;
        }
        return listaUI;
    }

    /// <summary>
    /// Mueve la selección al siguiente slot.
    /// </summary>
    [Button(ButtonSizes.Small)]
    public void Siguiente()
    {
        AsegurarInicializado();
        if (nodoActual == null) return;
        nodoActual = nodoActual.siguiente;
        AlCambiarInventario?.Invoke();
    }

    /// <summary>
    /// Mueve la selección al slot anterior.
    /// </summary>
    [Button(ButtonSizes.Small)]
    public void Anterior()
    {
        AsegurarInicializado();
        if (nodoActual == null) return;
        nodoActual = nodoActual.anterior;
        AlCambiarInventario?.Invoke();
    }

    /// <summary>
    /// Intenta añadir un ítem. Recorre la lista circular buscando espacio.
    /// </summary>
    [Button(ButtonSizes.Medium), GUIColor(0.4f, 0.8f, 1f)]
    public bool AnadirItem(Item nuevoItem, int cantidadAAgregar = 1)
    {
        if (nuevoItem == null || cantidadAAgregar <= 0) return false;

        AsegurarInicializado();
        if (nodoActual == null) return false;

        NodoInventario buscador = nodoActual;
        int visitados = 0;

        // 1. Intentar apilar en slots existentes recorriendo el círculo
        do
        {
            if (buscador.slot.PuedeApilar(nuevoItem))
            {
                int espacioDisponible = nuevoItem.maxStack - buscador.slot.cantidad;
                int cantidadAInsertar = Mathf.Min(cantidadAAgregar, espacioDisponible);

                buscador.slot.cantidad += cantidadAInsertar;
                cantidadAAgregar -= cantidadAInsertar;

                if (cantidadAAgregar == 0)
                {
                    AlCambiarInventario?.Invoke();
                    return true;
                }
            }
            buscador = buscador.siguiente;
            visitados++;
        } while (visitados < capacidad);

        // 2. Buscar slots vacíos recorriendo el círculo
        buscador = nodoActual;
        visitados = 0;
        while (cantidadAAgregar > 0 && visitados < capacidad)
        {
            if (buscador.slot.item == null)
            {
                buscador.slot.item = nuevoItem;
                int cantidadAInsertar = Mathf.Min(cantidadAAgregar, nuevoItem.maxStack);
                buscador.slot.cantidad = cantidadAInsertar;
                cantidadAAgregar -= cantidadAInsertar;
            }
            buscador = buscador.siguiente;
            visitados++;
        }

        AlCambiarInventario?.Invoke();
        return cantidadAAgregar == 0;
    }

    /// <summary>
    /// Quita una cantidad específica de un ítem recorriendo la lista.
    /// </summary>
    public bool QuitarItem(Item itemAQuitar, int cantidadAQuitar = 1)
    {
        if (itemAQuitar == null) return false;

        AsegurarInicializado();
        if (nodoActual == null) return false;

        NodoInventario buscador = nodoActual;
        int visitados = 0;
        int cantidadInicial = cantidadAQuitar;

        do
        {
            if (buscador.slot.item == itemAQuitar)
            {
                if (buscador.slot.cantidad >= cantidadAQuitar)
                {
                    buscador.slot.cantidad -= cantidadAQuitar;
                    if (buscador.slot.cantidad == 0) buscador.slot.Vaciar();
                    AlCambiarInventario?.Invoke();
                    return true;
                }
                else
                {
                    cantidadAQuitar -= buscador.slot.cantidad;
                    buscador.slot.Vaciar();
                }
            }
            buscador = buscador.siguiente;
            visitados++;
        } while (visitados < capacidad && cantidadAQuitar > 0);

        AlCambiarInventario?.Invoke();
        return cantidadAQuitar == 0;
    }
}
