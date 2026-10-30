/// <summary>
/// Implementa esta interfaz en cualquier objeto con el que la Paloma pueda interactuar.
/// El jugador la busca con un raycast desde la camara al presionar Interact (E).
/// </summary>
public interface IInteractable
{
    /// <summary>Texto para mostrar en pantalla, por ejemplo "Abrir puerta".</summary>
    string Prompt { get; }

    /// <summary>Se llama cuando el jugador interactua con este objeto.</summary>
    void Interact(inputsPaloma player);
}
