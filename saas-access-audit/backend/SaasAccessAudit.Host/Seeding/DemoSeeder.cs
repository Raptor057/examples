using System.Diagnostics;
using System.Globalization;
using Access.Contracts;
using Access.Domain.Entities;
using Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Persistence;

namespace SaasAccessAudit.Host.Seeding;

/// <summary>
/// SIEMBRA DE DEMOSTRACION. Es andamiaje del ejemplo, no parte del patron, y por eso vive en el
/// Host: es el unico lugar que conoce a los dos modulos a la vez. Ponerla dentro de uno obligaria
/// a que ese modulo supiera de las entidades del otro solo para sembrar.
///
/// El volumen tampoco es decorado. Con veinte renglones, una bitacora paginada en el servidor y
/// una filtrada en el cliente se ven igual de rapidas y el ejemplo no demuestra nada; con ciento
/// cincuenta mil, el filtro que falta en el servidor se nota en la primera pagina.
/// </summary>
public sealed class DemoSeeder(
    IServiceScopeFactory scopeFactory,
    ITenantContextAccessor tenantAccessor,
    IConfiguration configuration,
    ILogger<DemoSeeder> logger)
{
    private static readonly (string Code, string Name, string Group, string[] Permissions)[] RoleTemplates =
    [
        ("admin", "Administrador", "saas-admins",
        [
            PermissionCatalog.UsersView, PermissionCatalog.UsersDeactivate,
            PermissionCatalog.RolesView, PermissionCatalog.RolesManage,
            PermissionCatalog.AuditLogView
        ]),

        // Puede actuar sobre usuarios pero NO puede cambiar permisos ni leer la bitacora.
        ("operations", "Operacion", "saas-ops",
        [
            PermissionCatalog.UsersView, PermissionCatalog.UsersDeactivate, PermissionCatalog.RolesView
        ]),

        // Ve todo y no puede tocar nada. Es el contraste que hace visible el cruce de patrones.
        ("auditor", "Auditoria", "saas-auditors",
        [
            PermissionCatalog.UsersView, PermissionCatalog.RolesView, PermissionCatalog.AuditLogView
        ]),

        ("viewer", "Consulta", "saas-viewers", [PermissionCatalog.UsersView])
    ];

    private static readonly (string Username, string DisplayName, string Group)[] NamedUsers =
    [
        ("ana.torres", "Ana Torres", "saas-admins"),
        ("bruno.diaz", "Bruno Diaz", "saas-ops"),
        ("carla.mena", "Carla Mena", "saas-auditors"),
        ("dario.luna", "Dario Luna", "saas-viewers")
    ];

    private static readonly string[] FirstNames =
    [
        "Elena", "Fabian", "Gabriela", "Hector", "Irene", "Joaquin", "Karla", "Luis",
        "Marina", "Nestor", "Olivia", "Pablo", "Rocio", "Sergio", "Tania", "Ulises"
    ];

    private static readonly string[] LastNames =
    [
        "Alvarez", "Bermudez", "Castro", "Duarte", "Escobar", "Ferrer", "Gaitan", "Herrera",
        "Iriarte", "Jaramillo", "Klein", "Lozano", "Montes", "Navarro", "Ochoa", "Pineda"
    ];

    private static readonly string[] DeactivationReasons =
    [
        "Baja voluntaria confirmada por recursos humanos.",
        "Fin de contrato con el proveedor externo.",
        "Cuenta compartida detectada en la revision trimestral.",
        "Cambio de area: ya no requiere acceso a la consola.",
        "Solicitud del responsable del equipo por inactividad.",
        "Incidente de seguridad: credenciales expuestas."
    ];

    private static readonly string[] RoleChangeReasons =
    [
        "Ajuste de permisos aprobado en el comite de accesos.",
        "Separacion de funciones exigida por la auditoria externa.",
        "El rol necesitaba consultar la bitacora para el cierre de mes.",
        "Se retira el permiso destructivo tras la revision de riesgos.",
        "Alta de un equipo nuevo de operacion."
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var targetAuditEntries = configuration.GetValue("Seed:TargetAuditEntries", 120_000);
        var targetDenialEntries = configuration.GetValue("Seed:TargetDenialEntries", 30_000);
        var batch = configuration.GetValue("Seed:Batch", 5_000);
        var days = configuration.GetValue("Seed:Days", 180);
        var usersPerTenant = configuration.GetValue("Seed:UsersPerTenant", 40);
        var force = configuration.GetValue("Seed:Force", false);

        await using (var probeScope = scopeFactory.CreateAsyncScope())
        {
            var probe = probeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await probe.Set<Tenant>().AnyAsync(cancellationToken).ConfigureAwait(false) && !force)
            {
                logger.LogInformation("Seed omitido: la base ya tiene tenants. Usa Seed:Force=true para repetirlo.");
                return;
            }
        }

        var stopwatch = Stopwatch.StartNew();

        // Dos tenants con volumenes distintos: asi se nota que la bitacora de uno no muestra ni un
        // renglon de la otra, y que el TOTAL de cada pantalla tampoco lo delata.
        var plans = new[]
        {
            new TenantPlan("acme", "ACME Retail", 0.65),
            new TenantPlan("globex", "Globex Comercial", 0.35)
        };

        var tenantIds = await CreateTenantsAsync(plans, cancellationToken).ConfigureAwait(false);

        foreach (var plan in plans)
        {
            var tenantId = tenantIds[plan.Code];

            // El seeder opera COMO el tenant: establece el contexto en vez de saltarse los
            // filtros. Asi la siembra recorre el mismo camino que la aplicacion.
            tenantAccessor.Current = new TenantContext(tenantId);
            try
            {
                var users = await SeedTenantAccessAsync(plan, tenantId, usersPerTenant, cancellationToken)
                    .ConfigureAwait(false);

                await SeedAuditAsync(
                    plan, tenantId, users,
                    (int)(targetAuditEntries * plan.Share),
                    (int)(targetDenialEntries * plan.Share),
                    batch, days, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                tenantAccessor.Current = null;
            }
        }

        stopwatch.Stop();
        logger.LogInformation("Seed terminado en {Elapsed}.", stopwatch.Elapsed);
    }

    private async Task<Dictionary<string, long>> CreateTenantsAsync(
        TenantPlan[] plans, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var plan in plans)
        {
            var existing = await db.Set<Tenant>()
                .FirstOrDefaultAsync(tenant => tenant.Code == plan.Code, cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                result[plan.Code] = existing.Id;
                continue;
            }

            var tenant = new Tenant { Code = plan.Code, Name = plan.Name };
            db.Set<Tenant>().Add(tenant);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            result[plan.Code] = tenant.Id;
            logger.LogInformation("Tenant {Code} creado con id {TenantId}.", plan.Code, tenant.Id);
        }

        return result;
    }

    /// <summary>
    /// Roles, mapeo de pertenencias, concesiones y usuarios de un tenant.
    ///
    /// Fijate en el orden: primero se crean los roles, luego se les CONCEDEN permisos del
    /// catalogo ya sembrado. Sembrar el catalogo solo hace los permisos asignables; si este paso
    /// no existiera, ningun rol tendria nada y la consola se veria vacia para todo el mundo.
    /// </summary>
    private async Task<List<SeededUser>> SeedTenantAccessAsync(
        TenantPlan plan, long tenantId, int usersPerTenant, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var permissionIds = await db.Set<Permission>()
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var template in RoleTemplates)
        {
            var role = new Role { TenantId = tenantId, Code = template.Code, Name = template.Name };
            db.Set<Role>().Add(role);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            db.Set<GroupRoleMapping>().Add(new GroupRoleMapping
            {
                TenantId = tenantId,
                GroupName = template.Group,
                RoleId = role.Id
            });

            foreach (var code in template.Permissions)
            {
                db.Set<RolePermission>().Add(new RolePermission
                {
                    TenantId = tenantId,
                    RoleId = role.Id,
                    PermissionId = permissionIds[code]
                });
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Semilla fija por tenant: dos corridas producen los mismos datos, y eso hace que los
        // numeros del README se puedan reproducir.
        var random = new Random(plan.Code.GetHashCode(StringComparison.Ordinal));
        var users = new List<AppUser>();
        var memberships = new List<(AppUser User, string Group)>();

        foreach (var named in NamedUsers)
        {
            var user = new AppUser
            {
                TenantId = tenantId,
                Username = named.Username,
                DisplayName = named.DisplayName,
                Email = $"{named.Username}@{plan.Code}.example"
            };
            users.Add(user);
            memberships.Add((user, named.Group));
        }

        // El resto llena la lista para que la paginacion y la busqueda tengan de donde agarrarse.
        var groupWeights = new[] { "saas-viewers", "saas-viewers", "saas-viewers", "saas-ops", "saas-auditors" };
        for (var index = users.Count; index < usersPerTenant; index++)
        {
            var first = FirstNames[random.Next(FirstNames.Length)];
            var last = LastNames[random.Next(LastNames.Length)];
            var username = string.Create(
                CultureInfo.InvariantCulture,
                $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}{index:D2}");

            var user = new AppUser
            {
                TenantId = tenantId,
                Username = username,
                DisplayName = $"{first} {last}",
                Email = $"{username}@{plan.Code}.example"
            };
            users.Add(user);
            memberships.Add((user, groupWeights[random.Next(groupWeights.Length)]));
        }

        db.Set<AppUser>().AddRange(users);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (user, group) in memberships)
        {
            db.Set<AppUserGroup>().Add(new AppUserGroup
            {
                TenantId = tenantId,
                UserId = user.Id,
                GroupName = group
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[{Tenant}] acceso listo: {Roles} roles, {Users} usuarios.",
            plan.Code, RoleTemplates.Length, users.Count);

        return memberships
            .Select(item => new SeededUser(
                item.User.Id, item.User.PublicId, item.User.Username, item.User.DisplayName, item.Group))
            .ToList();
    }

    /// <summary>
    /// El volumen de bitacora. Se escribe por lotes con la deteccion de cambios apagada y un
    /// DbContext nuevo por lote: sin esos dos ajustes, el ChangeTracker recorre cada entidad ya
    /// agregada en cada Add y la siembra pasa de segundos a decenas de minutos.
    ///
    /// La fecha SI se asigna aqui, a diferencia del camino real donde la pone el motor: hace falta
    /// repartir los renglones en meses para que los filtros de rango tengan algo que filtrar.
    /// </summary>
    private async Task SeedAuditAsync(
        TenantPlan plan,
        long tenantId,
        List<SeededUser> users,
        int targetActions,
        int targetDenials,
        int batchSize,
        int days,
        CancellationToken cancellationToken)
    {
        var random = new Random(plan.Code.GetHashCode(StringComparison.Ordinal) ^ 7919);
        var periodStart = DateTime.UtcNow.AddDays(-days);
        var periodSeconds = days * 86_400d;

        var actors = users.Where(user => user.Group is "saas-admins" or "saas-ops").ToList();
        var subjects = users.Where(user => user.Group == "saas-viewers").ToList();
        var deniers = users
            .Where(user => user.Group is "saas-viewers" or "saas-ops" or "saas-auditors")
            .ToList();

        var stopwatch = Stopwatch.StartNew();
        var created = 0;

        while (created < targetActions)
        {
            var take = Math.Min(batchSize, targetActions - created);

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var entries = new List<ActionAuditEntry>(take);
            for (var index = 0; index < take; index++)
            {
                var actor = actors[random.Next(actors.Count)];
                var occurredAt = periodStart.AddSeconds(random.NextDouble() * periodSeconds);

                // Dos de cada tres renglones son desactivaciones (con efecto externo); el resto
                // son cambios de permisos (sin efecto externo). Esa mezcla es la que hace que el
                // filtro de pendientes tenga sentido y que la columna admita los tres estados:
                // aplicado, pendiente y "no aplicaba".
                if (random.Next(3) < 2)
                {
                    var subject = subjects[random.Next(subjects.Count)];
                    var applied = random.Next(100) >= 18;

                    entries.Add(new ActionAuditEntry
                    {
                        TenantId = tenantId,
                        ActionCode = PermissionCatalog.UsersDeactivate,
                        SubjectType = "user",
                        SubjectKey = subject.PublicId.ToString(),
                        SubjectLabel = subject.Username,
                        Reason = DeactivationReasons[random.Next(DeactivationReasons.Length)],
                        PerformedBy = actor.Username,
                        PerformedByDisplay = actor.DisplayName,
                        OccurredAtUtc = occurredAt,
                        ExternalEffectName = "identity-provider:revoke-sessions",
                        ExternalEffectApplied = applied,
                        ExternalEffectError = applied
                            ? null
                            : "El proveedor de identidad respondio 503: no fue posible revocar las sesiones."
                    });
                }
                else
                {
                    var template = RoleTemplates[random.Next(RoleTemplates.Length)];
                    var permission = PermissionCatalog.All[random.Next(PermissionCatalog.All.Count)];
                    var granted = random.Next(2) == 0;

                    entries.Add(new ActionAuditEntry
                    {
                        TenantId = tenantId,
                        ActionCode = PermissionCatalog.RolesManage,
                        SubjectType = "role-permission",
                        SubjectKey = $"{template.Code}:{permission.Code}",
                        SubjectLabel =
                            $"{template.Name} / {permission.DisplayName} ({(granted ? "concedido" : "revocado")})",
                        Reason = RoleChangeReasons[random.Next(RoleChangeReasons.Length)],
                        PerformedBy = actor.Username,
                        PerformedByDisplay = actor.DisplayName,
                        OccurredAtUtc = occurredAt

                        // Sin efecto externo: la columna queda en null, que NO es lo mismo que
                        // "quedo pendiente". El indice filtrado solo mira los false.
                    });
                }
            }

            db.Set<ActionAuditEntry>().AddRange(entries);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            created += take;
            logger.LogInformation(
                "[{Tenant}] bitacora de acciones {Created}/{Target} ({Rate:N0}/s)",
                plan.Code, created, targetActions, created / Math.Max(1d, stopwatch.Elapsed.TotalSeconds));
        }

        var deniedCreated = 0;
        while (deniedCreated < targetDenials)
        {
            var take = Math.Min(batchSize, targetDenials - deniedCreated);

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var entries = new List<AccessDenialEntry>(take);
            for (var index = 0; index < take; index++)
            {
                var actor = deniers[random.Next(deniers.Count)];
                var occurredAt = periodStart.AddSeconds(random.NextDouble() * periodSeconds);

                // Uno de cada cuatro renglones es de MODO AUDITORIA: no se bloqueo, se habria
                // bloqueado. Es la lista que alguien revisa antes de encender el flag, y sin
                // renglones asi el filtro de la pantalla no tendria nada que mostrar.
                var enforcementEnabled = random.Next(4) != 0;
                var target = DenialTargets[random.Next(DenialTargets.Length)];

                entries.Add(new AccessDenialEntry
                {
                    TenantId = tenantId,
                    PermissionCode = target.PermissionCode,
                    Route = target.Route,
                    HttpMethod = target.Method,
                    AttemptedBy = actor.Username,
                    AttemptedByDisplay = actor.DisplayName,
                    EnforcementEnabled = enforcementEnabled,
                    Blocked = enforcementEnabled,
                    OccurredAtUtc = occurredAt
                });
            }

            db.Set<AccessDenialEntry>().AddRange(entries);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            deniedCreated += take;
            logger.LogInformation(
                "[{Tenant}] bitacora de accesos {Created}/{Target}",
                plan.Code, deniedCreated, targetDenials);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "[{Tenant}] bitacora lista: {Actions} acciones y {Denials} rechazos en {Elapsed:hh\\:mm\\:ss}.",
            plan.Code, created, deniedCreated, stopwatch.Elapsed);
    }

    private static readonly (string PermissionCode, string Route, string Method)[] DenialTargets =
    [
        (PermissionCatalog.UsersDeactivate, "/api/access/users/00000000-0000-0000-0000-000000000000/deactivate", "POST"),
        (PermissionCatalog.RolesManage, "/api/access/roles/00000000-0000-0000-0000-000000000000/permissions", "POST"),
        (PermissionCatalog.AuditLogView, "/api/audit/actions", "GET"),
        (PermissionCatalog.AuditLogView, "/api/audit/denials", "GET"),
        (PermissionCatalog.RolesView, "/api/access/roles", "GET")
    ];

    private sealed record TenantPlan(string Code, string Name, double Share);

    private sealed record SeededUser(long Id, Guid PublicId, string Username, string DisplayName, string Group);
}
