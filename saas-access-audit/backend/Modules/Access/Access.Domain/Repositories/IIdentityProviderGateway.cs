namespace Access.Domain.Repositories;

/// <summary>Resultado del efecto externo. Nunca lanza: el fallo es un dato, no una excepcion.</summary>
public sealed record ExternalEffectResult(bool Applied, string? Error)
{
    public static ExternalEffectResult Ok() => new(true, null);

    public static ExternalEffectResult Failed(string error) => new(false, error);
}

/// <summary>
/// El sistema EXTERNO de la historia: el proveedor de identidad donde viven las sesiones.
///
/// Desactivar a alguien en nuestra base no lo saca de sus sesiones abiertas; eso hay que pedirlo
/// del otro lado. Y ese paso NO PUEDE COMPARTIR TRANSACCION con el nuestro: es otro servidor,
/// otra red y otro dueno. Puede fallar con la desactivacion ya escrita, y cuando eso pasa:
///
///   - la desactivacion NO se revierte (para cuando lo sabes, ya cambiaste el estado que
///     necesitarias para reponerlo, y revertir dejaria al usuario activo sin que nadie lo sepa),
///   - la bitacora registra que quedo pendiente y con que error,
///   - la respuesta lo dice, y la pantalla lo muestra como ADVERTENCIA y no como exito.
/// </summary>
public interface IIdentityProviderGateway
{
    Task<ExternalEffectResult> RevokeSessionsAsync(string username, CancellationToken cancellationToken = default);
}
