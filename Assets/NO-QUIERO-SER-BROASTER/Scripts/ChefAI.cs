using UnityEngine;
using UnityEngine.AI;
public class ChefAI : MonoBehaviour
{
    public enum Estado { Patrulla, Sospecha, Persecucion, Aturdido }
    public Estado estadoActual = Estado.Patrulla;

    [Header("Referencias")]
    public Transform paloma;
    public Transform[] puntosPatrulla;

    [Header("Velocidades")]
    public float velPatrulla = 2f;
    public float velPersecucion = 5f;

    [Header("Campo de vision")]
    public float rangoVision = 10f;
    [Range(0, 360)] public float anguloVision = 90f;
    public LayerMask capaObstaculos;

    [Header("Otros")]
    public float tiempoPerdidaPaloma = 3f;
    public float tiempoAturdido = 3f;
    public float distanciaAtrapar = 1.2f;

    private NavMeshAgent agente;
    private int indicePatrulla = 0;
    private float temporizador;
    private Vector3 ultimaPosicionVista;
    void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        // Si no arrastraste la paloma, la busca por tag
        if (paloma == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) paloma = obj.transform;
        }

        IrAlSiguientePunto();
    }
    void Update()
    {
        if (paloma == null) return;

        bool veAlaPaloma = PuedeVerPaloma();

        switch (estadoActual)
        {
            case Estado.Patrulla:
                agente.speed = velPatrulla;
                if (veAlaPaloma) CambiarEstado(Estado.Persecucion);
                else if (!agente.pathPending && agente.remainingDistance < 0.5f)
                    IrAlSiguientePunto();
                break;

            case Estado.Persecucion:
                agente.speed = velPersecucion;
                if (veAlaPaloma)
                {
                    ultimaPosicionVista = paloma.position;
                    agente.SetDestination(paloma.position);
                    temporizador = tiempoPerdidaPaloma;

                    if (Vector3.Distance(transform.position, paloma.position) < distanciaAtrapar)
                        AtraparPaloma();
                }
                else
                {
                    agente.SetDestination(ultimaPosicionVista);
                    temporizador -= Time.deltaTime;
                    if (temporizador <= 0) CambiarEstado(Estado.Sospecha);
                }
                break;

            case Estado.Sospecha:
                transform.Rotate(0, 90 * Time.deltaTime, 0);
                temporizador -= Time.deltaTime;
                if (veAlaPaloma) CambiarEstado(Estado.Persecucion);
                else if (temporizador <= 0) CambiarEstado(Estado.Patrulla);
                break;

            case Estado.Aturdido:
                temporizador -= Time.deltaTime;
                if (temporizador <= 0) CambiarEstado(Estado.Persecucion);
                break;
        }
    }
    bool PuedeVerPaloma()
    {
        Vector3 origen = transform.position + Vector3.up * 1f;
        Vector3 objetivo = paloma.position + Vector3.up * 0.5f;
        Vector3 dir = objetivo - origen;
        float distancia = dir.magnitude;

        if (distancia > rangoVision) return false;
        if (Vector3.Angle(transform.forward, dir) > anguloVision / 2f) return false;
        if (Physics.Raycast(origen, dir.normalized, distancia, capaObstaculos)) return false;

        return true;
    }
    void CambiarEstado(Estado nuevo)
    {
        estadoActual = nuevo;
        switch (nuevo)
        {
            case Estado.Patrulla:
                IrAlSiguientePunto();
                break;
            case Estado.Sospecha:
                agente.ResetPath();
                temporizador = 3f;
                break;
            case Estado.Aturdido:
                agente.ResetPath();
                temporizador = tiempoAturdido;
                break;
            case Estado.Persecucion:
                temporizador = tiempoPerdidaPaloma;
                // aqui va el grito: "¡SE ME ESCAPA EL POLLO!"
                break;
        }
    }
    void IrAlSiguientePunto()
    {
        if (puntosPatrulla.Length == 0) return;
        agente.SetDestination(puntosPatrulla[indicePatrulla].position);
        indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
    }
    void AtraparPaloma()
    {
        Debug.Log("¡TE HICIERON BROASTER!");
        // aqui llamas a tu GameManager para la pantalla de derrota
    }
    public void Aturdir()
    {
        CambiarEstado(Estado.Aturdido);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoVision);
        Vector3 izq = Quaternion.Euler(0, -anguloVision / 2f, 0) * transform.forward;
        Vector3 der = Quaternion.Euler(0, anguloVision / 2f, 0) * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, izq * rangoVision);
        Gizmos.DrawRay(transform.position, der * rangoVision);
    }
}