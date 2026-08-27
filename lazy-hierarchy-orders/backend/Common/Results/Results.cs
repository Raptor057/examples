namespace Common.Results;

/// <summary>Exito sin datos.</summary>
public interface ISuccess;

/// <summary>Exito con payload. El presenter decide como se proyecta al envelope.</summary>
public interface ISuccess<out TData> : ISuccess
{
    TData Data { get; }
}

/// <summary>Fallo de negocio esperado: viaja como HTTP 200 con isSuccess = false.</summary>
public interface IFailure
{
    string Message { get; }
}

/// <summary>Datos de entrada invalidos: HTTP 400.</summary>
public interface IValidationFailure : IFailure;

/// <summary>Registro inexistente: HTTP 404.</summary>
public interface INotFoundFailure : IFailure;

/// <summary>Conflicto de estado: HTTP 409.</summary>
public interface IConflictFailure : IFailure;
