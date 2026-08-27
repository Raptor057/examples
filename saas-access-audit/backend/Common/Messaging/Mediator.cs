using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Messaging;

/// <summary>
/// Despachador por reflexion: resuelve el handler concreto del contenedor a partir del tipo
/// del request. El contenedor es la unica fuente de handlers, y todos se registran a mano
/// (regla de oro 5): un registro que falta revienta aqui, en tiempo de ejecucion, con un
/// mensaje que dice exactamente que interfaz no estaba registrada.
/// </summary>
public sealed class Mediator(IServiceProvider provider) : IMediator
{
    private static readonly ConcurrentDictionary<(Type Request, Type Response), MethodInfo> HandleMethods = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        where TResponse : IResponse
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No hay handler registrado para {handlerType}. Registralo en el ServiceCollectionEx de su capa Application.");

        var method = HandleMethods.GetOrAdd((requestType, typeof(TResponse)),
            _ => handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!);

        return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken)
    {
        // El tipo estatico manda: los presenters se registran sobre el tipo BASE de la respuesta.
        var handlers = provider.GetServices<INotificationHandler<TNotification>>();
        foreach (var handler in handlers)
            await handler.Handle(notification, cancellationToken).ConfigureAwait(false);
    }
}

public static class MediatorServiceCollectionEx
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        // Sin esta linea el proyecto compila, los controllers existen, y toda peticion da 500.
        services.AddScoped<IMediator, Mediator>();
        return services;
    }
}
