using Microsoft.AspNetCore.Authorization;

namespace LabelPrinting.Host.Access;

public static class PermissionPolicies
{
    /// <summary>El claim que lleva cada permiso concedido.</summary>
    public const string PermissionClaim = "permission";

    /// <summary>
    /// Registra una politica por permiso, con el mismo nombre. Asi el endpoint escribe
    /// <c>[Authorize(Policy = PermissionCatalog.Print)]</c> y no hay que mantener dos listas.
    /// </summary>
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services, bool enforce)
    {
        services.AddAuthorizationBuilder()
            // Nada queda expuesto por olvido: lo que no declare politica exige, al menos, estar
            // autenticado. Es lo contrario de lo habitual -donde el endpoint sin atributo es
            // anonimo- y es lo que evita que un endpoint nuevo nazca abierto.
            .SetFallbackPolicy(enforce
                ? new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()
                : new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());

        foreach (var permission in PermissionCatalog.All)
        {
            services.AddAuthorizationBuilder().AddPolicy(permission, policy =>
            {
                if (enforce) policy.RequireClaim(PermissionClaim, permission);

                // Modo auditoria: las politicas existen y los endpoints ya las declaran, pero
                // dejan pasar. Sirve para encender permisos en un sistema que ya tiene
                // consumidores, sin romperlos el mismo dia.
                else policy.RequireAssertion(_ => true);
            });
        }

        return services;
    }
}
