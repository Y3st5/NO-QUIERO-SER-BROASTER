using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Pinta el inventario (hotbar estilo Minecraft) leyendo los datos de <see cref="Inventario"/>.
///
/// - Muestra TODOS los slots del inventario (o los que indique <see cref="slotsVisibles"/>).
/// - Se refresca solo cuando el inventario dispara <c>AlCambiarInventario</c>.
/// - Permite elegir slot con las teclas 1..9 / 0, la rueda del mouse o haciendo clic.
/// - Muestra el nombre del ítem seleccionado al cambiarlo.
///
/// Coloca este componente junto a un <see cref="UIDocument"/> que use InventoryMain.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class InventoryUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Inventario inventory;

    [Header("Hotbar")]
    [Tooltip("Cuántos slots dibujar. 0 = todos los que tenga el inventario.")]
    [Min(0)] [SerializeField] private int slotsVisibles = 0;

    [Tooltip("Muestra el nombre del ítem al cambiar de slot.")]
    [SerializeField] private bool mostrarNombreAlCambiar = true;

    [Tooltip("Duración en segundos del nombre del ítem.")]
    [SerializeField] private float duracionNombre = 1.6f;

    private VisualElement root;
    private VisualElement slotContainer;
    private Label itemNameLabel;

    private readonly List<VisualElement> slotElements = new List<VisualElement>();

    private int selectedIndex = 0;
    private bool initialized;
    private float hideNameAt = -1f;

    // ------------------------------------------------------------------
    // CICLO DE VIDA
    // ------------------------------------------------------------------
    private void OnEnable()
    {
        ResolveReferences();

        if (inventory != null)
            inventory.AlCambiarInventario += RefreshUI;

        // Si el UIDocument ya está listo lo usamos; si no, Start() lo hará.
        TryInitialize();
    }

    private void Start()
    {
        TryInitialize();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.AlCambiarInventario -= RefreshUI;
    }

    private void Update()
    {
        if (!initialized) return;

        HandleHotbarInput();

        if (hideNameAt > 0f && Time.unscaledTime >= hideNameAt)
        {
            hideNameAt = -1f;
            if (itemNameLabel != null)
                itemNameLabel.RemoveFromClassList("visible");
        }
    }

    // ------------------------------------------------------------------
    // INICIALIZACIÓN
    // ------------------------------------------------------------------
    private void ResolveReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                inventory = player.GetComponentInChildren<Inventario>(true);

            if (inventory == null)
                inventory = FindAnyObjectByType<Inventario>();
        }

        if (uiDocument == null)
            Debug.LogError("[InventoryUI] No hay ningún UIDocument en este GameObject.", this);

        if (inventory == null)
            Debug.LogError("[InventoryUI] No se encontró el componente Inventario en la escena.", this);
    }

    private void TryInitialize()
    {
        if (uiDocument == null || inventory == null) return;

        if (initialized)
        {
            RefreshUI();
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        slotContainer = root.Q<VisualElement>("SlotContainer");
        itemNameLabel = root.Q<Label>("ItemNameLabel");

        if (slotContainer == null)
        {
            Debug.LogError(
                "[InventoryUI] No se encontró el elemento 'SlotContainer'. " +
                "Revisa que el UIDocument use InventoryMain.uxml.", this);
            return;
        }

        initialized = true;
        RefreshUI();
    }

    // ------------------------------------------------------------------
    // DIBUJADO
    // ------------------------------------------------------------------
    private void RefreshUI()
    {
        if (!initialized || slotContainer == null) return;

        slotContainer.Clear();
        slotElements.Clear();

        int total = inventory.slots.Count;
        int toShow = slotsVisibles <= 0 ? total : Mathf.Min(slotsVisibles, total);

        for (int i = 0; i < toShow; i++)
        {
            VisualElement slot = CreateSlotElement(inventory.slots[i], i);
            slotContainer.Add(slot);
            slotElements.Add(slot);
        }

        // Mantenemos una selección válida
        if (toShow == 0)
            selectedIndex = 0;
        else if (selectedIndex >= toShow)
            selectedIndex = toShow - 1;

        ApplySelection(false);
    }

    private VisualElement CreateSlotElement(SlotInventario data, int index)
    {
        VisualElement slot = new VisualElement();
        slot.AddToClassList("hotbar-slot");
        slot.pickingMode = PickingMode.Position;

        // Clic sobre el slot -> lo selecciona
        int slotIndex = index;
        slot.RegisterCallback<MouseDownEvent>(_ => SelectSlot(slotIndex, true));

        Item item = data != null ? data.item : null;

        if (item != null)
        {
            if (item.icono != null)
            {
                VisualElement icon = new VisualElement();
                icon.AddToClassList("item-icon");
                icon.pickingMode = PickingMode.Ignore;
                icon.style.backgroundImage = new StyleBackground(item.icono);
                slot.Add(icon);
            }
            else
            {
                // Sin sprite todavía: mostramos la inicial para que se note que hay ítem
                Label fallback = new Label(ObtenerInicial(item));
                fallback.AddToClassList("item-fallback");
                fallback.pickingMode = PickingMode.Ignore;
                slot.Add(fallback);
            }

            if (data.cantidad > 1)
            {
                Label quantity = new Label(data.cantidad.ToString());
                quantity.AddToClassList("item-quantity");
                quantity.pickingMode = PickingMode.Ignore;
                slot.Add(quantity);
            }
        }

        Label keyHint = new Label(ObtenerEtiquetaTecla(index));
        keyHint.AddToClassList("slot-key");
        keyHint.pickingMode = PickingMode.Ignore;
        slot.Add(keyHint);

        return slot;
    }

    // ------------------------------------------------------------------
    // SELECCIÓN
    // ------------------------------------------------------------------
    private void HandleHotbarInput()
    {
        if (slotElements.Count == 0) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0, true);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1, true);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2, true);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot(3, true);
            if (keyboard.digit5Key.wasPressedThisFrame) SelectSlot(4, true);
            if (keyboard.digit6Key.wasPressedThisFrame) SelectSlot(5, true);
            if (keyboard.digit7Key.wasPressedThisFrame) SelectSlot(6, true);
            if (keyboard.digit8Key.wasPressedThisFrame) SelectSlot(7, true);
            if (keyboard.digit9Key.wasPressedThisFrame) SelectSlot(8, true);
            if (keyboard.digit0Key.wasPressedThisFrame) SelectSlot(9, true);
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int direction = scroll > 0f ? 1 : -1;
                int next = (selectedIndex + direction + slotElements.Count) % slotElements.Count;
                SelectSlot(next, true);
            }
        }
    }

    /// <summary>Selecciona un slot. Si <paramref name="avisar"/> es true muestra el nombre del ítem.</summary>
    public void SelectSlot(int index, bool avisar = true)
    {
        if (slotElements.Count == 0) return;
        if (index < 0 || index >= slotElements.Count) return;

        selectedIndex = index;
        ApplySelection(avisar);
    }

    /// <summary>Devuelve el slot seleccionado actualmente (útil para "usar ítem").</summary>
    public SlotInventario SlotSeleccionado
    {
        get
        {
            if (inventory == null || selectedIndex < 0 || selectedIndex >= inventory.slots.Count)
                return null;

            return inventory.slots[selectedIndex];
        }
    }

    private void ApplySelection(bool showName)
    {
        for (int i = 0; i < slotElements.Count; i++)
            slotElements[i].EnableInClassList("is-selected", i == selectedIndex);

        if (itemNameLabel == null) return;

        SlotInventario slot = SlotSeleccionado;
        Item item = slot != null ? slot.item : null;

        if (!mostrarNombreAlCambiar || item == null)
        {
            itemNameLabel.RemoveFromClassList("visible");
            hideNameAt = -1f;
            return;
        }

        itemNameLabel.text = string.IsNullOrEmpty(item.nombre) ? item.name : item.nombre;

        if (showName)
        {
            itemNameLabel.AddToClassList("visible");
            hideNameAt = Time.unscaledTime + Mathf.Max(0.1f, duracionNombre);
        }
    }

    // ------------------------------------------------------------------
    // UTILIDADES
    // ------------------------------------------------------------------
    private static string ObtenerEtiquetaTecla(int index)
    {
        if (index < 9) return (index + 1).ToString();
        if (index == 9) return "0";
        return string.Empty;
    }

    private static string ObtenerInicial(Item item)
    {
        string nombre = string.IsNullOrEmpty(item.nombre) ? item.name : item.nombre;
        return string.IsNullOrEmpty(nombre) ? "?" : nombre.Substring(0, 1).ToUpperInvariant();
    }
}
