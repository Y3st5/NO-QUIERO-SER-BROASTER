/// <summary>
/// Implementa esta interfaz en cualquier objeto con el que el jugador pueda interactuar con E.
/// </summary>
public interface IInteractable
{
    /// <summary>Texto para mostrar en pantalla, por ejemplo "Abrir puerta".</summary>
    string Prompt { get; }

    /// <summary>Se llama cuando el jugador presiona E mirando este objeto.</summary>
    void Interact(InputsPlayer player);
}