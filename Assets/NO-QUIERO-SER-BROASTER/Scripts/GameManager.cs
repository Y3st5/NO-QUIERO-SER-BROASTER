using UnityEngine;
using System;

/// <summary>
/// Estado de la partida: vidas, derrota y victoria.
/// Expone eventos para que un HUD (TextMeshPro, UI Toolkit, etc.) pueda reaccionar
/// sin tener que consultar la Paloma directamente.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Paloma vidaPaloma;
    [SerializeField] private ChefAI chefAI;

    [Header("Victoria")]
    [Tooltip("Segundos que hay que sobrevivir para ganar. 0 = victoria desactivada " +
             "(también puedes llamar a Ganar() desde otro script).")]
    [SerializeField] private float tiempoSupervivenciaParaGanar = 0f;

    public enum GameState { Playing, Won, Lost }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    /// <summary>True cuando la partida ya se ganó o se perdió.</summary>
    public bool PartidaTerminada => CurrentState != GameState.Playing;

    /// <summary>Vidas actuales de la Paloma (se mantiene sincronizada con Paloma.OnVidaCambiada).</summary>
    public int VidasActuales { get; private set; }

    /// <summary>Vidas máximas configuradas en la Paloma.</summary>
    public int VidasMaximas { get; private set; }

    /// <summary>Segundos jugados desde que empezó la partida.</summary>
    public float TiempoJugado => tiempoJugado;

    /// <summary>(vidasActuales, vidasMaximas) cada vez que cambian las vidas.</summary>
    public event Action<int, int> OnVidasCambiadas;

    /// <summary>Se dispara una sola vez al terminar: true = victoria, false = derrota.</summary>
    public event Action<bool> OnPartidaTerminada;

    private float tiempoJugado;

    private void Start()
    {
        // Inicializar el estado del cursor al empezar la partida
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (chefAI == null)
            chefAI = FindAnyObjectByType<ChefAI>();

        if (vidaPaloma != null)
        {
            VidasActuales = vidaPaloma.Vidas;
            VidasMaximas = vidaPaloma.VidasMax;

            vidaPaloma.OnDerrota += HandlePlayerDefeat;
            vidaPaloma.OnVidaCambiada += HandleVidaCambiada;

            // Valor inicial para que el HUD pueda pintarse sin esperar al primer golpe.
            OnVidasCambiadas?.Invoke(VidasActuales, VidasMaximas);
        }
    }

    private void OnDestroy()
    {
        if (vidaPaloma != null)
        {
            vidaPaloma.OnDerrota -= HandlePlayerDefeat;
            vidaPaloma.OnVidaCambiada -= HandleVidaCambiada;
        }
    }

    private void Update()
    {
        if (PartidaTerminada)
            return;

        tiempoJugado += Time.deltaTime;

        if (tiempoSupervivenciaParaGanar > 0f && tiempoJugado >= tiempoSupervivenciaParaGanar)
            Ganar();
    }

    private void HandleVidaCambiada(int vidas, int vidasMax)
    {
        VidasActuales = vidas;
        VidasMaximas = vidasMax;

        OnVidasCambiadas?.Invoke(vidas, vidasMax);
    }

    private void HandlePlayerDefeat()
    {
        if (PartidaTerminada)
            return;

        CurrentState = GameState.Lost;
        Debug.Log("GAME OVER: ¡TE HICIERON BROASTER!");

        TerminarPartida();
        OnPartidaTerminada?.Invoke(false);
    }

    /// <summary>
    /// Fuerza la victoria. Úsalo desde un trigger de meta, al cumplir un objetivo, etc.
    /// </summary>
    public void Ganar()
    {
        if (PartidaTerminada)
            return;

        CurrentState = GameState.Won;
        Debug.Log("¡VICTORIA! La Paloma sobrevivió al Chef.");

        TerminarPartida();
        OnPartidaTerminada?.Invoke(true);
    }

    /// <summary>Congela la partida: cursor libre, Chef detenido e inputs desactivados.</summary>
    private void TerminarPartida()
    {
        // 1. Gestionar el Cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Detener la IA del Chef
        if (chefAI == null)
            chefAI = FindAnyObjectByType<ChefAI>();

        if (chefAI != null)
            chefAI.Detener();

        // 3. Desactivar inputs de la Paloma
        if (vidaPaloma != null)
        {
            inputsPaloma control = vidaPaloma.GetComponent<inputsPaloma>();
            if (control != null) control.enabled = false;
        }
    }
}
