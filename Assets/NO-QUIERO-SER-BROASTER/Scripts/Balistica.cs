using UnityEngine;

/// <summary>
/// Utilidades de tiro parabólico.
/// Permite calcular la velocidad inicial necesaria para que un proyectil llegue
/// a un punto en un tiempo dado, predecir un blanco en movimiento y dibujar el arco.
/// </summary>
public static class Balistica
{
    /// <summary>
    /// Calcula la velocidad inicial (v0) necesaria para que un proyectil sometido a "gravedad"
    /// salga de "origen" y llegue a "destino" exactamente en "tiempoVuelo" segundos.
    /// Fórmula: v0 = (destino - origen - 0.5 * g * t^2) / t
    /// </summary>
    public static bool TryCalcularVelocidad(Vector3 origen, Vector3 destino, float tiempoVuelo, Vector3 gravedad, out Vector3 velocidad)
    {
        velocidad = Vector3.zero;

        if (tiempoVuelo <= 0.0001f)
            return false;

        velocidad = (destino - origen - 0.5f * gravedad * tiempoVuelo * tiempoVuelo) / tiempoVuelo;
        return true;
    }

    /// <summary>
    /// Devuelve la posición futura del objetivo aplicando un factor de predicción (0 = sin predecir, 1 = lead total).
    /// Por defecto ignora la velocidad vertical, para que saltar siga siendo una forma válida de esquivar.
    /// </summary>
    public static Vector3 Predecir(Vector3 posicion, Vector3 velocidadObjetivo, float tiempo, float factor, bool predecirVertical = false)
    {
        Vector3 velocidad = velocidadObjetivo;
        if (!predecirVertical)
            velocidad.y = 0f;

        return posicion + velocidad * (tiempo * factor);
    }

    /// <summary>
    /// Rellena "destino" con las muestras del arco (incluye origen y punto final). Útil para LineRenderer o Gizmos.
    /// </summary>
    public static void MuestrearArco(Vector3 origen, Vector3 velocidad, Vector3 gravedad, float tiempo, Vector3[] destino)
    {
        if (destino == null || destino.Length == 0)
            return;

        int ultimo = destino.Length - 1;

        for (int i = 0; i <= ultimo; i++)
        {
            float t = ultimo == 0 ? 0f : tiempo * i / ultimo;
            destino[i] = origen + velocidad * t + 0.5f * gravedad * t * t;
        }
    }
}
