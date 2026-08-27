using Access.Application.Dtos;
using Access.Application.UseCases.DeactivateUser.Responses;
using Access.Contracts;
using Access.Domain.Repositories;
using Audit.Contracts;
using Common.Messaging;

namespace Access.Application.UseCases.DeactivateUser;

/// <summary>
/// EL CRUCE DE LOS DOS PATRONES, EN UN SOLO ARCHIVO.
///
/// Para llegar hasta aqui la peticion ya paso la politica del endpoint
/// ([HasPermission(PermissionCatalog.UsersDeactivate)]). Este handler no vuelve a preguntar quien
/// eres ni compara contra nombres de rol: eso ya se decidio, y decidirlo otra vez dentro de la
/// logica de negocio es el antipatron que la regla prohibe.
///
/// Lo que si hace es dejar rastro. Y el ORDEN de los tres pasos no es casual:
///
///   1. Lo transaccional  - desactivar. Un solo SaveChanges: o queda, o no paso nada.
///   2. El efecto externo - revocar sesiones en el proveedor de identidad. FUERA de la
///                          transaccion, porque es otro sistema. Si falla, NO se revierte el
///                          paso 1: para cuando lo sabes ya cambiaste el estado que necesitarias
///                          para reponerlo, y revertir dejaria al usuario activo sin que nadie
///                          se entere.
///   3. La bitacora       - registra la accion Y si el paso 2 quedo aplicado. Va al final
///                          justamente porque necesita saber como termino el paso 2.
///
/// Y el fallo del paso 2 SE REPORTA HACIA ARRIBA: la respuesta lo dice y la pantalla lo pinta
/// como advertencia. Un exito verde ahi seria mentir.
/// </summary>
internal sealed class DeactivateUserHandler(
    IAccessWriteRepository repository,
    IIdentityProviderGateway identityProvider,
    IActionAuditLog auditLog)
    : IRequestHandler<DeactivateUserRequest, DeactivateUserResponse>
{
    private const string ExternalEffectName = "identity-provider:revoke-sessions";

    public async Task<DeactivateUserResponse> Handle(DeactivateUserRequest request, CancellationToken cancellationToken)
    {
        var (reason, reasonError) = ReasonPolicy.Normalize(request.Reason);
        if (reason is null)
            return new DeactivateUserValidationFailure(reasonError!);

        // La busqueda va por EF, asi que el global query filter ya la acota al tenant del token:
        // pedir el identificador de un usuario de otra empresa devuelve "no existe", no sus datos.
        var user = await repository.FindUserAsync(request.UserPublicId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return new DeactivateUserNotFoundFailure("No se encontro ese usuario.");

        // Paso 1. La escritura filtra por el ESTADO PREVIO: dos clics no la ejecutan dos veces.
        var deactivated = await repository.DeactivateUserAsync(user, cancellationToken).ConfigureAwait(false);
        if (!deactivated)
            return new DeactivateUserConflictFailure("Ese usuario ya estaba desactivado.");

        // Paso 2. Fuera de la transaccion. No lanza: el fallo es un dato que hay que registrar.
        var externalEffect = await identityProvider
            .RevokeSessionsAsync(user.Username, cancellationToken)
            .ConfigureAwait(false);

        // Paso 3. Quien actuo NO viaja en el record: lo pone la bitacora desde el token.
        await auditLog.RecordAsync(
            new ActionAuditRecord(
                ActionCode: PermissionCatalog.UsersDeactivate,
                SubjectType: "user",
                SubjectKey: user.PublicId.ToString(),
                SubjectLabel: user.Username,
                Reason: reason,
                ExternalEffectName: ExternalEffectName,
                ExternalEffectApplied: externalEffect.Applied,
                ExternalEffectError: externalEffect.Error),
            cancellationToken).ConfigureAwait(false);

        return new DeactivateUserSuccess(new DeactivateUserResultDto(
            user.PublicId,
            user.Username,
            externalEffect.Applied,
            externalEffect.Error));
    }
}
