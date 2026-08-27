namespace Common.Messaging;

// Libreria mediadora minima del ejemplo. En los proyectos reales del catalogo esto vive en
// una libreria compartida (submodulo Common) que NO se modifica desde el repositorio del
// proyecto; aqui se incluye completa para que el ejemplo sea ejecutable sin dependencias
// privadas. La forma de los tipos es la misma: Request -> Handler -> Response -> Presenter.

/// <summary>Marca el resultado de un caso de uso. Nunca es una entidad de dominio.</summary>
public interface IResponse;

/// <summary>Entrada de un caso de uso. Es un record inmutable, no un DTO de transporte HTTP.</summary>
public interface IRequest<out TResponse> where TResponse : IResponse;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Los presenters implementan esta interfaz sobre el tipo BASE de respuesta del caso de uso.
/// Publicar la respuesta es lo que llena el view model; sin presenter el endpoint responde vacio.
/// </summary>
public interface INotificationHandler<in TNotification>
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        where TResponse : IResponse;

    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken);
}
