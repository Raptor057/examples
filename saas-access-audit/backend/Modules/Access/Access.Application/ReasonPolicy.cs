namespace Access.Application;

/// <summary>
/// El motivo, validado en UN solo lugar.
///
/// Existe porque toda accion sensible lo pide y porque la regla es la misma para todas: si cada
/// handler lo validara a su manera, el primero que lo dejara pasar vacio meteria en la bitacora
/// un renglon que no responde la pregunta para la que la bitacora existe.
///
/// Esta validacion NO sustituye a la restriccion de la base. La base lleva un CHECK de no vacio
/// sobre la columna, y es ahi donde queda cubierto el camino nuevo que alguien escriba manana sin
/// pasar por aqui.
/// </summary>
public static class ReasonPolicy
{
    public const int MinLength = 5;

    public const int MaxLength = 500;

    /// <summary>
    /// Normaliza y valida. Devuelve el motivo limpio o el mensaje de por que no sirve.
    /// El texto se recorta antes de medirlo: cinco espacios no son un motivo.
    /// </summary>
    public static (string? Reason, string? Error) Normalize(string? raw)
    {
        var reason = (raw ?? string.Empty).Trim();

        if (reason.Length == 0)
            return (null, "El motivo es obligatorio.");

        if (reason.Length < MinLength)
            return (null, $"El motivo tiene que decir algo: al menos {MinLength} caracteres.");

        if (reason.Length > MaxLength)
            return (null, $"El motivo no puede pasar de {MaxLength} caracteres.");

        return (reason, null);
    }
}
