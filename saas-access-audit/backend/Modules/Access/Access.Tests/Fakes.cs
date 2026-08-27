using Access.Domain.Entities;
using Access.Domain.Models;
using Access.Domain.Repositories;
using Audit.Contracts;
using SaasAccessAudit.Kernel;

namespace Access.Tests;

/// <summary>
/// Dobles de prueba escritos a mano. No hay libreria de mocks a proposito: estas interfaces son
/// pequenas, y un doble explicito deja a la vista QUE se espera de cada colaborador.
/// </summary>
internal sealed class FakeAccessWriteRepository : IAccessWriteRepository
{
    public AppUser? User { get; set; }

    public Role? Role { get; set; }

    public Permission? Permission { get; set; }

    public int DeactivateCalls { get; private set; }

    public List<(long RoleId, long PermissionId, bool Granted)> Grants { get; } = [];

    public bool NextSetReturnsChanged { get; set; } = true;

    public Task<AppUser?> FindUserAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(User is not null && User.PublicId == publicId ? User : null);

    public Task<Role?> FindRoleAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Role is not null && Role.PublicId == publicId ? Role : null);

    public Task<Permission?> FindPermissionAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(Permission is not null && Permission.Code == code ? Permission : null);

    public Task<bool> DeactivateUserAsync(AppUser user, CancellationToken cancellationToken = default)
    {
        DeactivateCalls++;
        if (!user.IsActive) return Task.FromResult(false);

        user.IsActive = false;
        user.DeactivatedAtUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> SetRolePermissionAsync(
        long roleId, long permissionId, bool granted, CancellationToken cancellationToken = default)
    {
        Grants.Add((roleId, permissionId, granted));
        return Task.FromResult(NextSetReturnsChanged);
    }
}

internal sealed class FakeActionAuditLog : IActionAuditLog
{
    public List<ActionAuditRecord> Records { get; } = [];

    public Task RecordAsync(ActionAuditRecord record, CancellationToken cancellationToken = default)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }
}

internal sealed class FakeAccessDecisionLog : IAccessDecisionLog
{
    public List<AccessDenialRecord> Records { get; } = [];

    public Task RecordDenialAsync(AccessDenialRecord record, CancellationToken cancellationToken = default)
    {
        Records.Add(record);
        return Task.CompletedTask;
    }
}

internal sealed class FakeIdentityProviderGateway(bool applied, string? error = null) : IIdentityProviderGateway
{
    public int Calls { get; private set; }

    public Task<ExternalEffectResult> RevokeSessionsAsync(
        string username, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(applied ? ExternalEffectResult.Ok() : ExternalEffectResult.Failed(error ?? "fallo"));
    }
}

internal sealed class FakeUserContextAccessor(UserContext? current = null) : IUserContextAccessor
{
    public UserContext? Current { get; set; } = current;
}

internal sealed class FakeAccessControlSettings(bool enforce) : IAccessControlSettings
{
    public bool EnforcePermissions { get; set; } = enforce;
}

internal sealed class FakeAccessReadRepository : IAccessReadRepository
{
    public List<string> Permissions { get; set; } = [];

    public int EffectivePermissionCalls { get; private set; }

    public Task<IReadOnlyList<string>> GetEffectivePermissionCodesAsync(
        IReadOnlyList<string> groupNames, CancellationToken cancellationToken = default)
    {
        EffectivePermissionCalls++;
        return Task.FromResult<IReadOnlyList<string>>(groupNames.Count == 0 ? [] : Permissions);
    }

    public Task<string> GetTenantCodeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult("acme");

    public Task<IReadOnlyList<RoleWithGrants>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleWithGrants>>([]);

    public Task<IReadOnlyList<PermissionRow>> GetPermissionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PermissionRow>>([]);

    public Task<PagedResult<UserRow>> GetUsersPageAsync(
        string? search, bool onlyActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(PagedResult<UserRow>.Empty(pageNumber, pageSize));
}
