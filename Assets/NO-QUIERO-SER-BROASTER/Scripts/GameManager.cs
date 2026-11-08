using UnityEngine;
using System;
using Sirenix.OdinInspector;

/// <summary>
/// Estado de la partida: vidas, derrota y victoria.
/// Expone eventos para que un HUD (TextMeshPro, UI Toolkit, etc.) pueda reaccionar
/// sin tener que consultar la Paloma directamente.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Title("Referencias")]
    [Required("El GameManager necesita una referencia a la Paloma para controlar la partida.")]
    [SerializeField] private Paloma vidaPaloma;

    [SerializeField] private ChefAI chefAI;

    [Title("Condiciones de Victoria")]
    [Tooltip("Segundos que hay que sobrevivir para ganar. 0 = victoria desactivada")]
    [MinValue(0)]
    [SerializeField] private float tiempoSupervivenciaParaGanar = 0f;

    [BoxGroup("Estado de la Partida")]
    public enum GameState { Playing, Won, Lost }

    [BoxGroup("Estado de la Partida")]
    [ReadOnly] [SerializeField] private GameState _currentState = GameState.Playing;
    public GameState CurrentState { get => _currentState; private set => _currentState = value; }

    [BoxGroup("Estado de la Partida")]
    [ReadOnly]
    public bool PartidaTerminada => CurrentState != GameState.Playing;

    [BoxGroup("Estado de la Partida")]
    [ReadOnly] [SerializeField] private int _vidasActuales;
    public int VidasActuales { get => _vidasActuales; private set => _vidasActuales = value; }

    [BoxGroup("Estado de la Partida")]
    [ReadOnly] [SerializeField] private int _vidasMaximas;
    public int VidasMaximas { get => _vidasMaximas; private set => _vidasMaximas = value; }

    [BoxGroup("Estado de la Partida")]
    [ReadOnly]
    public float TiempoJugado => tiempoJugado;

    /// <summary>(vidasActuales, vidasMaximas) cada vez que cambian las vidas.</summary>
    public event Action<int, int> OnVidasCambiadas;

    /// <summary>Se dispara una sola vez al terminar: true = victoria, false = derrota.</summary>
    public event Action<bool> OnPartidaTerminada;

    private float tiempoJugado;

    [Title("Controles de Debug")]
    [Button("Forzar Victoria")]
    [GUIColor(0.4f, 1f, 0.4f)]
    public void Ganar()
    {
        if (PartidaTerminada)
            return;

        CurrentState = GameState.Won;
        Debug.Log("¡VICTORIA! La Paloma sobrevivió al Chef.");

        TerminarPartida();
        OnPartidaTerminada?.Invoke(true);
    }

    [Button("Forzar Derrota")]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ForzarDerrota()
    {
        if (PartidaTerminada)
            return;

        CurrentState = GameState.Lost;
        Debug.Log("GAME OVER: ¡TE HICIERON BROASTER!");

        TerminarPartida();
        OnPartidaTerminada?.Invoke(false);
    }

    private void Start()
    {
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

    private void TerminarPartida()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (chefAI == null)
            chefAI = FindAnyObjectByType<ChefAI>();

        if (chefAI != null)
            chefAI.Detener();

        if (vidaPaloma != null)
        {
            inputsPaloma control = vidaPaloma.GetComponent<inputsPaloma>();
            if (control != null) control.enabled = false;
        }
    }
}
