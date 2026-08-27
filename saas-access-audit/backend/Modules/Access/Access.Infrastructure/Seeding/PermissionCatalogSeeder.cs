using Access.Contracts;
using Access.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaasAccessAudit.Shared.Persistence;

namespace Access.Infrastructure.Seeding;

/// <summary>
/// EL SEMBRADOR DEL CATALOGO. Esto NO es andamiaje del ejemplo: es parte del patron y corre en
/// CADA arranque, tambien en produccion.
///
/// Compara el catalogo declarado en codigo contra la tabla y hace tres cosas:
///   - inserta lo que falta,
///   - actualiza el texto de lo que cambio,
///   - DESACTIVA lo que ya no esta en el codigo.
///
/// Esa tercera es la que cierra la puerta: un permiso dado de alta a mano en la base, o uno que
/// se quito del codigo, queda desactivado en el siguiente arranque y deja de poder concederse.
/// Asi el codigo es la unica fuente de verdad de que permisos existen, y desplegar una funcion
/// nueva no depende de que alguien se acuerde de correr un INSERT en cada ambiente.
///
/// Lo que NO hace, y conviene saberlo antes de reportar que "la funcion no se desplego": conceder.
/// Sembrar un permiso solo lo hace ASIGNABLE. Si ningun rol lo tiene, la opcion se esconde para
/// todos y parece que el despliegue no llego.
/// </summary>
public sealed class PermissionCatalogSeeder(AppDbContext db, ILogger<PermissionCatalogSeeder> logger)
{
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters para alcanzar tambien los permisos desactivados: si un codigo vuelve
        // al catalogo hay que reactivar SU fila, no insertar una segunda que chocaria con la
        // llave unica. Permission es entidad global, asi que aqui no hay filtro de tenant que
        // reponer.
        var existing = await db.Set<Permission>()
            .IgnoreQueryFilters()
            .ToDictionaryAsync(permission => permission.Code, cancellationToken)
            .ConfigureAwait(false);

        var inserted = 0;
        var updated = 0;

        foreach (var definition in PermissionCatalog.All)
        {
            if (!existing.TryGetValue(definition.Code, out var permission))
            {
                db.Set<Permission>().Add(new Permission
                {
                    Code = definition.Code,
                    Module = definition.Module,
                    Resource = definition.Resource,
                    Action = definition.Action,
                    DisplayName = definition.DisplayName,
                    IsDestructive = definition.IsDestructive,
                    IsActive = true
                });
                inserted++;
                continue;
            }

            var changed =
                permission.Module != definition.Module
                || permission.Resource != definition.Resource
                || permission.Action != definition.Action
                || permission.DisplayName != definition.DisplayName
                || permission.IsDestructive != definition.IsDestructive
                || !permission.IsActive;

            if (!changed) continue;

            permission.Module = definition.Module;
            permission.Resource = definition.Resource;
            permission.Action = definition.Action;
            permission.DisplayName = definition.DisplayName;
            permission.IsDestructive = definition.IsDestructive;
            permission.IsActive = true;
            permission.DeletedAtUtc = null;
            updated++;
        }

        // Lo que la base tiene y el codigo ya no declara. Se desactiva, no se borra: las
        // concesiones viejas y los renglones de bitacora que lo mencionan siguen siendo legibles.
        var retired = 0;
        foreach (var permission in existing.Values.Where(item => item.IsActive && !PermissionCatalog.Contains(item.Code)))
        {
            permission.IsActive = false;
            permission.DeletedAtUtc = DateTime.UtcNow;
            retired++;
        }

        if (inserted + updated + retired == 0)
        {
            logger.LogInformation("Catalogo de permisos al dia: {Count} permisos.", PermissionCatalog.All.Count);
            return;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Catalogo de permisos sincronizado: {Inserted} nuevos, {Updated} actualizados, {Retired} retirados.",
            inserted, updated, retired);
    }
}
