using System;
using MoreMountains.Feedbacks;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Vidas de la Paloma (3 por defecto).
/// Gestiona la invulnerabilidad temporal, el feedback de daño (Feel) y la derrota.
/// </summary>
public class Paloma : MonoBehaviour
{
    [Title("Configuración de Vidas")]
    [SerializeField]
    [MinValue(1)]
    private int vidasMax = 3;

    [Tooltip("Segundos de invulnerabilidad tras recibir un golpe.")]
    [SerializeField]
    private float invulnerabilidad = 1f;

    [Title("Feedback de Daño (Feel)")]
    [Tooltip("Se reproduce al recibir daño: parpadeo rojo + sacudida de cámara.")]
    [SerializeField]
    public MMF_Player damageFeedback;

    [Title("Estado Actual")]
    [ReadOnly]
    public int Vidas { get; private set; }

    [ReadOnly]
    public int VidasMax => vidasMax;

    [ReadOnly]
    public bool Derrotada { get; private set; }

    [ReadOnly]
    public bool Invulnerable => Time.time < finInvulnerabilidad;

    /// <summary>(vidasActuales, vidasMax) cada vez que cambian las vidas.</summary>
    public event Action<int, int> OnVidaCambiada;

    /// <summary>Se dispara una sola vez al llegar a 0 vidas.</summary>
    public event Action OnDerrota;

    private float finInvulnerabilidad = -1f;

    private void Awake()
    {
        Vidas = Mathf.Max(1, vidasMax);

        if (damageFeedback == null)
            damageFeedback = GetComponent<MMF_Player>();
    }

    /// <summary>
    /// Quita vidas si el jugador no está derrotado ni invulnerable.
    /// Devuelve true si el golpe se aplicó.
    /// </summary>
    [Button("Simular Daño")]
    public bool RecibirDanio(int cantidad = 1)
    {
        if (Derrotada || Invulnerable || cantidad <= 0)
            return false;

        Vidas = Mathf.Max(0, Vidas - cantidad);
        finInvulnerabilidad = Time.time + Mathf.Max(0f, invulnerabilidad);

        if (damageFeedback != null)
            damageFeedback.PlayFeedbacks();

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

        // El GameManager ahora se encarga de los inputs, el cursor y la IA.
        OnDerrota?.Invoke();
    }
}
