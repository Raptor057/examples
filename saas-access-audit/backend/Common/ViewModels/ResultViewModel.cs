namespace Common.ViewModels;

/// <summary>
/// El envelope unico del ecosistema: { data, isSuccess, message, utcTimeStamp }.
/// Se registra por controller (open generic) para que cada peticion tenga el suyo,
/// y lo llena EXCLUSIVAMENTE el presenter del caso de uso.
/// </summary>
public class ResultViewModel<TController>
{
    public object? Data { get; private set; }

    public bool IsSuccess { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public DateTime UtcTimeStamp { get; private set; } = DateTime.UtcNow;

    public void Fail(string message)
    {
        Data = null;
        IsSuccess = false;
        Message = message ?? string.Empty;
        UtcTimeStamp = DateTime.UtcNow;
    }

    public void Set<TData>(Results.ISuccess<TData> success, Func<TData, object?> projection)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(projection);

        Data = projection(success.Data);
        IsSuccess = true;
        Message = string.Empty;
        UtcTimeStamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Cambia SOLO el mensaje, conservando data e isSuccess.
    ///
    /// Existe para el exito parcial: una accion que si ocurrio pero cuyo efecto externo quedo
    /// pendiente sigue siendo isSuccess = true -deshacerla no es una opcion- y aun asi tiene que
    /// llegar con un aviso al usuario. Sin esto, el presenter tendria que elegir entre mentir
    /// (verde y callado) o mentir al reves (rojo sobre algo que si paso).
    /// </summary>
    public void SetMessage(string message)
    {
        Message = message ?? string.Empty;
    }

    public void Set(string? message = null)
    {
        Data = null;
        IsSuccess = true;
        Message = message ?? string.Empty;
        UtcTimeStamp = DateTime.UtcNow;
    }
}
