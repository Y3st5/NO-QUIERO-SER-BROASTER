using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Charco que deja el proyectil del Chef al tocar el suelo.
/// Ralentiza a la Paloma mientras está encima, se desvanece y se autodestruye.
/// No usa triggers: comprueba con OverlapSphere, que funciona de forma fiable con CharacterController.
/// </summary>
public class Charco : MonoBehaviour
{
    [Header("Efecto")]
    [Tooltip("Radio dentro del cual ralentiza.")]
    [SerializeField] private float radio = 1.2f;

    [Tooltip("Multiplicador de velocidad mientras se pisa (0.45 = 55 % más lento).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float factorRalentizacion = 0.45f;

    [Tooltip("Cada cuánto se refresca la ralentización (debe ser mayor que el intervalo entre frames).")]
    [SerializeField] private float duracionRalentizacion = 0.15f;

    [Header("Vida")]
    [SerializeField] private float duracion = 8f;
    [SerializeField] private float duracionDesvanecido = 1.5f;

    [Header("Limite")]
    [Tooltip("Cuantos charcos pueden existir a la vez. El más antiguo se elimina.")]
    [SerializeField] private int maximoVivos = 5;

    private static readonly List<Charco> vivos = new List<Charco>();

    private readonly Collider[] buffer = new Collider[16];
    private float restante;
    private Vector3 escalaInicial;

    private void Awake()
    {
        restante = duracion;
        escalaInicial = transform.localScale;

        while (vivos.Count >= Mathf.Max(1, maximoVivos))
        {
            Charco viejo = vivos[0];
            vivos.RemoveAt(0);
            if (viejo != null)
                Destroy(viejo.gameObject);
        }

        vivos.Add(this);
    }

    private void OnDestroy()
    {
        vivos.Remove(this);
    }

    private void Update()
    {
        restante -= Time.deltaTime;

        if (restante <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        RalentizarPalomas();

        if (restante < duracionDesvanecido)
        {
            float t = Mathf.Clamp01(restante / duracionDesvanecido);
            transform.localScale = new Vector3(escalaInicial.x * t, escalaInicial.y, escalaInicial.z * t);
        }
    }

    private void RalentizarPalomas()
    {
        int encontrados = Physics.OverlapSphereNonAlloc(
            transform.position, radio, buffer, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < encontrados; i++)
        {
            Collider colision = buffer[i];
            if (colision == null)
                continue;

            inputsPaloma paloma = colision.GetComponentInParent<inputsPaloma>();
            if (paloma != null)
                paloma.Ralentizar(factorRalentizacion, duracionRalentizacion);
        }
    }
}
