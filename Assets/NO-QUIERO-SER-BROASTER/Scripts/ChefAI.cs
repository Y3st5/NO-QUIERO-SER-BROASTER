using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chef perseguidor de la Paloma.
/// Patrulla, sospecha, persigue, se aturde y lanza proyectiles en parábola
/// prediciendo hacia dónde se mueve el jugador.
/// </summary>
public class ChefAI : MonoBehaviour
{
    public enum Estado { Patrulla, Sospecha, Persecucion, Aturdido, Lanzando, Detenido }

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
    public float tiempoSospecha = 3f;
    public float distanciaAtrapar = 1.2f;
    public float cooldownAtrapar = 1.5f;

    [Header("Lanzamiento")]
    [Tooltip("Prefab del proyectil. Si está vacío, el Chef no lanza.")]
    public GameObject proyectilPrefab;
    [Tooltip("Punto desde el que sale el proyectil. Si está vacío se usa la posición del Chef + 1.5 m.")]
    public Transform puntoDeLanzamiento;
    [Tooltip("Altura a la que apunta sobre la base de la Paloma (la raíz está a los pies).")]
    public float alturaObjetivo = 1.2f;
    public float rangoMinLanzamiento = 4f;
    public float rangoMaxLanzamiento = 18f;
    [Tooltip("Velocidad de referencia para calcular el tiempo de vuelo.")]
    public float velocidadNominal = 12f;
    public float tiempoVueloMin = 0.6f;
    public float tiempoVueloMax = 1.1f;
    [Tooltip("Segundos mínimos entre lanzamientos.")]
    public float cadenciaLanzamiento = 2.5f;
    [Tooltip("Aviso previo antes de soltar el proyectil (telegraph).")]
    public float tiempoPreparacion = 0.6f;
    [Tooltip("0 = apunta a tu posición actual, 1 = predicción total. Se recomienda 0.6-0.7.")]
    [Range(0f, 1.5f)] public float factorPrediccion = 0.7f;
    [Tooltip("Error de apuntado aleatorio en metros (0 = francotirador).")]
    public float errorApuntado = 0.4f;

    private NavMeshAgent agente;
    private int indicePatrulla;
    private float temporizador;
    private float siguienteLanzamiento;
    private float siguienteAgarre;
    private Vector3 ultimaPosicionVista;
    private VidaPaloma vidaPaloma;
    private inputsPaloma jugador;
    private readonly Vector3[] arcoDebug = new Vector3[32];
    private bool estaRecargandoProyectil = false;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        // Si no arrastraste la paloma, la busca por tag
        if (paloma == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) paloma = obj.transform;
        }

        if (paloma != null)
        {
            vidaPaloma = paloma.GetComponent<VidaPaloma>();
            jugador = paloma.GetComponent<inputsPaloma>();
        }

        if (agente != null)
            IrAlSiguientePunto();
    }

    private void Update()
    {
        if (agente == null || paloma == null)
            return;

        if (estadoActual == Estado.Detenido)
            return;

        bool veAlaPaloma = PuedeVerPaloma();

        switch (estadoActual)
        {
            case Estado.Patrulla:
                agente.speed = velPatrulla;
                if (veAlaPaloma)
                {
                    CambiarEstado(Estado.Persecucion);
                }
                else if (!agente.pathPending && agente.hasPath &&
                         agente.remainingDistance <= agente.stoppingDistance + 0.5f)
                {
                    IrAlSiguientePunto();
                }
                break;

            case Estado.Persecucion:
                agente.speed = velPersecucion;
                if (veAlaPaloma)
                {
                    ultimaPosicionVista = paloma.position;
                    agente.SetDestination(paloma.position);
                    temporizador = tiempoPerdidaPaloma;

                    if (Vector3.Distance(transform.position, paloma.position) <= distanciaAtrapar)
                        AtraparPaloma();
                    else if (PuedeLanzar())
                    {
                        StartCoroutine(SecuenciaLanzamiento());
                    }
                }
                else
                {
                    agente.SetDestination(ultimaPosicionVista);
                    temporizador -= Time.deltaTime;
                    if (temporizador <= 0f)
                        CambiarEstado(Estado.Sospecha);
                }
                break;

            case Estado.Sospecha:
                transform.Rotate(0f, 90f * Time.deltaTime, 0f);
                temporizador -= Time.deltaTime;
                if (veAlaPaloma) CambiarEstado(Estado.Persecucion);
                else if (temporizador <= 0f) CambiarEstado(Estado.Patrulla);
                break;

            case Estado.Aturdido:
                temporizador -= Time.deltaTime;
                if (temporizador <= 0f) CambiarEstado(Estado.Persecucion);
                break;

            case Estado.Lanzando:
                GirarHacia(paloma.position);
                // El temporizador ahora lo maneja la corrutina, pero mantenemos 
                // la rotación aquí mientras estemos en el estado de Lanzando.
                break;
        }
    }

    // ---------------- VISION ----------------
    private bool PuedeVerPaloma()
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

    // ---------------- MAQUINA DE ESTADOS ----------------
    private void CambiarEstado(Estado nuevo)
    {
        estadoActual = nuevo;

        switch (nuevo)
        {
            case Estado.Patrulla:
                agente.isStopped = false;
                agente.ResetPath();
                IrAlSiguientePunto();
                break;

            case Estado.Sospecha:
                agente.ResetPath();
                agente.isStopped = true;
                temporizador = tiempoSospecha;
                break;

            case Estado.Aturdido:
                agente.ResetPath();
                agente.isStopped = true;
                temporizador = tiempoAturdido;
                break;

            case Estado.Persecucion:
                agente.isStopped = false;
                temporizador = tiempoPerdidaPaloma;
                break;

            case Estado.Lanzando:
                agente.isStopped = false; // Permitir que se mueva mientras prepara el disparo
                temporizador = tiempoPreparacion;
                break;

            case Estado.Detenido:
                agente.ResetPath();
                agente.isStopped = true;
                break;
        }
    }

    private void IrAlSiguientePunto()
    {
        if (agente == null || puntosPatrulla == null || puntosPatrulla.Length == 0)
            return;

        agente.isStopped = false;
        agente.SetDestination(puntosPatrulla[indicePatrulla].position);
        indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
    }

    // ---------------- ATRAPAR ----------------
    private void AtraparPaloma()
    {
        if (Time.time < siguienteAgarre)
            return;

        if (vidaPaloma == null || !vidaPaloma.RecibirDanio(1))
            return;

        siguienteAgarre = Time.time + Mathf.Max(0.1f, cooldownAtrapar);

        if (vidaPaloma.Derrotada)
            Detener();
    }

    // ---------------- LANZAMIENTO ----------------
    private bool PuedeLanzar()
    {
        if (proyectilPrefab == null || estaRecargandoProyectil)
            return false;

        float distancia = Vector3.Distance(transform.position, paloma.position);
        return distancia >= rangoMinLanzamiento && distancia <= rangoMaxLanzamiento;
    }

    private System.Collections.IEnumerator SecuenciaLanzamiento()
    {
        estaRecargandoProyectil = true;
        CambiarEstado(Estado.Lanzando);

        // 1. Tiempo de preparación (Telegraph)
        yield return new WaitForSeconds(tiempoPreparacion);

        // 2. Ejecutar el lanzamiento
        Lanzar();

        // 3. Esperar el cooldown (Cadencia)
        yield return new WaitForSeconds(cadenciaLanzamiento);

        estaRecargandoProyectil = false;
        
        // Volver a persecución si no ha sido interrumpido por otro estado
        if (estadoActual == Estado.Lanzando)
            CambiarEstado(Estado.Persecucion);
    }

    private void Lanzar()
    {
        if (proyectilPrefab == null)
            return;

        Vector3 origen = OrigenLanzamiento();
        Vector3 destino = CalcularDestino(origen);
        float tiempoVuelo = CalcularTiempoVuelo(Vector3.Distance(origen, destino));

        if (!Balistica.TryCalcularVelocidad(origen, destino, tiempoVuelo, Physics.gravity, out Vector3 velocidad))
            return;

        // Sale un poco por delante para no aparecer dentro del Chef
        Vector3 horizontal = destino - origen;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude > 0.01f)
            origen += horizontal.normalized * 0.4f;

        Quaternion rotacion = velocidad.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(velocidad, Vector3.up)
            : transform.rotation;

        GameObject instancia = Instantiate(proyectilPrefab, origen, rotacion);
        ProyectilChef proyectil = instancia.GetComponent<ProyectilChef>();

        if (proyectil != null)
        {
            proyectil.Inicializar(transform, velocidad);
        }
        else
        {
            Rigidbody cuerpo = instancia.GetComponent<Rigidbody>();
            if (cuerpo != null) cuerpo.linearVelocity = velocidad;
        }
    }


    private Vector3 OrigenLanzamiento()
    {
        return puntoDeLanzamiento != null
            ? puntoDeLanzamiento.position
            : transform.position + Vector3.up * 1.5f;
    }

    private Vector3 PosicionObjetivo()
    {
        return paloma.position + Vector3.up * alturaObjetivo;
    }

    private float CalcularTiempoVuelo(float distancia)
    {
        float tiempo = distancia / Mathf.Max(0.01f, velocidadNominal);
        return Mathf.Clamp(tiempo, tiempoVueloMin, tiempoVueloMax);
    }

    /// <summary>
    /// Predice el punto de impacto iterando: el tiempo de vuelo depende de la distancia
    /// y la distancia depende de dónde estará el jugador. Converge en 2-3 pasadas.
    /// </summary>
    private Vector3 CalcularDestino(Vector3 origen)
    {
        Vector3 objetivo = PosicionObjetivo();
        Vector3 velocidadJugador = jugador != null ? jugador.Velocity : Vector3.zero;
        Vector3 prediccion = objetivo;

        for (int i = 0; i < 3; i++)
        {
            float tiempoVuelo = CalcularTiempoVuelo(Vector3.Distance(origen, prediccion));
            prediccion = Balistica.Predecir(objetivo, velocidadJugador, tiempoVuelo, factorPrediccion);
        }

        if (errorApuntado > 0f)
        {
            Vector3 error = Random.insideUnitSphere * errorApuntado;
            error.y *= 0.5f;
            prediccion += error;
        }

        return prediccion;
    }

    private void GirarHacia(Vector3 posicion)
    {
        Vector3 dir = posicion - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 540f * Time.deltaTime);
    }

    // ---------------- API PUBLICA ----------------
    /// <summary>Aturde al Chef durante "tiempoAturdido" segundos.</summary>
    public void Aturdir()
    {
        if (estadoActual == Estado.Detenido || agente == null)
            return;

        CambiarEstado(Estado.Aturdido);
    }

    /// <summary>Detiene al Chef definitivamente (se llama al perder la partida).</summary>
    public void Detener()
    {
        if (agente == null)
        {
            enabled = false;
            return;
        }

        if (estadoActual != Estado.Detenido)
            CambiarEstado(Estado.Detenido);
    }

    // ---------------- GIZMOS ----------------
    private void OnDrawGizmosSelected()
    {
        // --- GIZMOS DE PATRULLA ---
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < puntosPatrulla.Length; i++)
            {
                if (puntosPatrulla[i] == null) continue;

                // Dibujar esfera RELLENA en cada punto de patrulla
                Gizmos.DrawSphere(puntosPatrulla[i].position, 0.3f);

                // Dibujar línea hacia el siguiente punto
                int siguiente = (i + 1) % puntosPatrulla.Length;
                if (puntosPatrulla[siguiente] != null)
                {
                    Gizmos.DrawLine(puntosPatrulla[i].position, puntosPatrulla[siguiente].position);
                }
            }
        }

        // --- GIZMOS DE VISIÓN ---
        Vector3 ojos = transform.position + Vector3.up * 1f;

        // 1. Dibujar el círculo de rango total (RELLENO y transparente)
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f); 
        Gizmos.DrawSphere(ojos, rangoVision); // Esfera rellena para dar volumen al rango

        // 2. Dibujar el cono de visión RELLENO
        Vector3 dirIzquierda = Quaternion.Euler(0, -anguloVision / 2f, 0) * transform.forward;
        Vector3 dirDerecha = Quaternion.Euler(0, anguloVision / 2f, 0) * transform.forward;

        // Dibujar el relleno del cono usando triángulos
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Amarillo con transparencia
        int segmentos = 15;
        for (int i = 0; i < segmentos; i++)
        {
            float t1 = (float)i / segmentos;
            float t2 = (float)(i + 1) / segmentos;
            Vector3 dir1 = Vector3.Slerp(dirIzquierda, dirDerecha, t1);
            Vector3 dir2 = Vector3.Slerp(dirIzquierda, dirDerecha, t2);

            // Dibujar el "triángulo" desde el ojo hasta el borde del rango
            // Nota: Gizmos no tiene DrawTriangle, así que simulamos el relleno con líneas densas
            // o simplemente dibujamos el borde más fuerte.
            Gizmos.DrawLine(ojos, ojos + dir1 * rangoVision);
        }
        
        // Dibujamos el borde del cono más fuerte para que se defina bien
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(ojos, dirIzquierda * rangoVision);
        Gizmos.DrawRay(ojos, dirDerecha * rangoVision);

        Vector3 puntoAnterior = ojos + dirIzquierda * rangoVision;
        for (int i = 1; i <= segmentos; i++)
        {
            float interpolation = (float)i / segmentos;
            Vector3 dirActual = Vector3.Slerp(dirIzquierda, dirDerecha, interpolation);
            Vector3 puntoActual = ojos + dirActual * rangoVision;
            Gizmos.DrawLine(puntoAnterior, puntoActual);
            puntoAnterior = puntoActual;
        }

        if (proyectilPrefab == null)
            return;

        Vector3 origen = OrigenLanzamiento();
        Vector3 destino = Application.isPlaying && paloma != null
            ? CalcularDestino(origen)
            : paloma != null
                ? PosicionObjetivo()
                : origen + transform.forward * 10f;

        float tiempoVuelo = CalcularTiempoVuelo(Vector3.Distance(origen, destino));

        if (!Balistica.TryCalcularVelocidad(origen, destino, tiempoVuelo, Physics.gravity, out Vector3 velocidad))
            return;

        Balistica.MuestrearArco(origen, velocidad, Physics.gravity, tiempoVuelo, arcoDebug);

        Gizmos.color = Color.cyan;
        for (int i = 1; i < arcoDebug.Length; i++)
            Gizmos.DrawLine(arcoDebug[i - 1], arcoDebug[i]);
    }
}
