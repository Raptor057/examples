namespace Printing.Domain.Rules;

/// <summary>
/// Cuando se reintenta un trabajo que fallo, y cuando se deja de reintentar.
///
/// Es una regla pura y por eso vive aqui: se prueba sin cola, sin base y sin reloj de verdad.
/// </summary>
public static class PrintQueueRules
{
    /// <summary>
    /// Intentos antes de darlo por muerto. Cinco no es un numero magico: con la espera de abajo
    /// son unos 21 minutos, que es mas o menos lo que tarda alguien en notar que una impresora se
    /// apago y volver a encenderla. Reintentar un dia entero solo acumula etiquetas que saldran
    /// todas juntas cuando nadie las espera.
    /// </summary>
    public const int MaxAttempts = 5;

    /// <summary>
    /// Espera creciente entre intentos, en segundos: 5s, 30s, 2min, 8min, y de ahi en adelante el
    /// tope.
    ///
    /// Crece a proposito. Una impresora que no contesta casi nunca se arregla en el segundo
    /// siguiente -esta apagada, sin papel o desconectada- y martillearla cada segundo llena el log
    /// sin acercar la solucion. El primer reintento va corto porque SI cubre el caso comun: un
    /// corte de red de un instante.
    /// </summary>
    public static readonly int[] BackoffSeconds = [5, 30, 120, 480, 1800];

    /// <summary>Cuando toca el siguiente intento, contado desde <paramref name="now"/>.</summary>
    /// <param name="attempts">Intentos YA realizados, incluido el que acaba de fallar.</param>
    public static DateTime NextAttemptAt(int attempts, DateTime now)
    {
        var index = Math.Clamp(attempts - 1, 0, BackoffSeconds.Length - 1);
        return now.AddSeconds(BackoffSeconds[index]);
    }

    /// <summary>Si ya no vale la pena reintentar.</summary>
    public static bool IsExhausted(int attempts) => attempts >= MaxAttempts;

    /// <summary>
    /// Cuanto tiempo se conserva un trabajo ya terminado antes de purgarlo. Los muertos se quedan
    /// mas: son los que alguien va a venir a mirar.
    /// </summary>
    public static readonly TimeSpan KeepSent = TimeSpan.FromDays(7);
    public static readonly TimeSpan KeepDead = TimeSpan.FromDays(30);
}
