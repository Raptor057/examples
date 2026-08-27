using Access.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Access.Infrastructure.Identity;

/// <summary>
/// El proveedor de identidad SIMULADO.
///
/// Es andamiaje del ejemplo: en un proyecto real aqui va una llamada HTTP al proveedor. Lo que si
/// es de verdad -y es el motivo de que exista- es su COMPORTAMIENTO: tarda, puede fallar, y su
/// fallo no puede tumbar lo que ya se escribio en nuestra base.
///
/// Falla de forma DETERMINISTA segun el nombre de usuario, no al azar: asi el paseo del README da
/// el mismo resultado dos veces y la pantalla siempre tiene renglones pendientes que mostrar. Un
/// fallo aleatorio haria imposible reproducir el ejemplo.
/// </summary>
internal sealed class FakeIdentityProviderGateway(
    IConfiguration configuration,
    ILogger<FakeIdentityProviderGateway> logger) : IIdentityProviderGateway
{
    private readonly int _failEveryNth = configuration.GetValue("Demo:IdentityProviderFailsEveryNthUser", 4);

    public async Task<ExternalEffectResult> RevokeSessionsAsync(
        string username, CancellationToken cancellationToken = default)
    {
        // Latencia simbolica: recordar que este paso sale de nuestro proceso.
        await Task.Delay(15, cancellationToken).ConfigureAwait(false);

        var fails = _failEveryNth > 0
            && Math.Abs(username.GetHashCode(StringComparison.Ordinal)) % _failEveryNth == 0;

        if (!fails)
        {
            logger.LogInformation("Sesiones revocadas en el proveedor de identidad para {Username}.", username);
            return ExternalEffectResult.Ok();
        }

        const string error = "El proveedor de identidad respondio 503: no fue posible revocar las sesiones.";

        // Se registra como advertencia y NO se lanza. Lanzar aqui haria que el llamador
        // pareciera un error del sistema cuando en realidad la desactivacion si ocurrio.
        logger.LogWarning("Efecto externo pendiente para {Username}: {Error}", username, error);
        return ExternalEffectResult.Failed(error);
    }
}
