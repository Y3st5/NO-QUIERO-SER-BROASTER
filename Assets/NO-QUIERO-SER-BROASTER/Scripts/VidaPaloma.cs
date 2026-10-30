using System;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Vidas de la Paloma (3 por defecto).
/// Gestiona la invulnerabilidad temporal, el feedback de daño (Feel) y la derrota.
/// </summary>
public class VidaPaloma : MonoBehaviour
{
    [Header("Vidas")]
    [SerializeField] private int vidasMax = 3;

    [Tooltip("Segundos de invulnerabilidad tras recibir un golpe.")]
    [SerializeField] private float invulnerabilidad = 1f;

    [Header("Feedback de daño (Feel)")]
    [Tooltip("Se reproduce al recibir daño: parpadeo rojo + sacudida de cámara.")]
    [SerializeField] private MMF_Player feedbackDanio;

    /// <summary>Vidas actuales.</summary>
    public int Vidas { get; private set; }

    /// <summary>Vidas máximas configuradas.</summary>
    public int VidasMax => vidasMax;

    /// <summary>True cuando ya no quedan vidas.</summary>
    public bool Derrotada { get; private set; }

    /// <summary>True mientras el jugador no puede recibir daño.</summary>
    public bool Invulnerable => Time.time < finInvulnerabilidad;

    /// <summary>(vidasActuales, vidasMax) cada vez que cambian las vidas.</summary>
    public event Action<int, int> OnVidaCambiada;

    /// <summary>Se dispara una sola vez al llegar a 0 vidas.</summary>
    public event Action OnDerrota;

    private float finInvulnerabilidad = -1f;

    private void Awake()
    {
        Vidas = Mathf.Max(1, vidasMax);

        if (feedbackDanio == null)
            feedbackDanio = GetComponent<MMF_Player>();
    }

    /// <summary>
    /// Quita vidas si el jugador no está derrotado ni invulnerable.
    /// Devuelve true si el golpe se aplicó.
    /// </summary>
    public bool RecibirDanio(int cantidad = 1)
    {
        if (Derrotada || Invulnerable || cantidad <= 0)
            return false;

        Vidas = Mathf.Max(0, Vidas - cantidad);
        finInvulnerabilidad = Time.time + Mathf.Max(0f, invulnerabilidad);

        if (feedbackDanio != null)
            feedbackDanio.PlayFeedbacks();

        OnVidaCambiada?.Invoke(Vidas, vidasMax);

        if (Vidas == 0)
            Derrotar();

        return true;
    }

    private void Derrotar()
    {
        if (Derrotada)
            return;

        Derrotada = true;

        // Se libera el cursor desde el OnDisable de inputsPaloma.
        inputsPaloma control = GetComponent<inputsPaloma>();
        if (control != null)
            control.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ChefAI chef = FindAnyObjectByType<ChefAI>();
        if (chef != null)
            chef.Detener();

        OnDerrota?.Invoke();

        Debug.Log("GAME OVER: ¡TE HICIERON BROASTER!");
    }
}
