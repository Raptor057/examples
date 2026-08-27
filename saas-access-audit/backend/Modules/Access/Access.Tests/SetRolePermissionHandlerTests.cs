using Access.Application.UseCases.SetRolePermission;
using Access.Application.UseCases.SetRolePermission.Responses;
using Access.Contracts;
using Access.Domain.Entities;

namespace Access.Tests;

/// <summary>
/// Cambiar permisos no borra un dato y aun asi es una accion sensible: cambia lo que OTRAS
/// personas pueden hacer. Aqui se prueba lo que la hace segura -que el catalogo sea la unica
/// fuente- y lo que la hace auditable.
/// </summary>
public sealed class SetRolePermissionHandlerTests
{
    private static readonly Guid RoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static (SetRolePermissionHandler Handler, FakeAccessWriteRepository Repository, FakeActionAuditLog Log) Build()
    {
        var repository = new FakeAccessWriteRepository
        {
            Role = new Role { Id = 5, PublicId = RoleId, Code = "operations", Name = "Operacion" },
            Permission = new Permission
            {
                Id = 9,
                Code = PermissionCatalog.AuditLogView,
                DisplayName = "Consultar la bitacora"
            }
        };

        var log = new FakeActionAuditLog();
        return (new SetRolePermissionHandler(repository, log), repository, log);
    }

    [Fact]
    public async Task No_se_puede_conceder_un_permiso_que_el_codigo_no_conoce()
    {
        // Sin esta defensa, la fila quedaria en la tabla, la pantalla lo mostraria concedido, y
        // ningun endpoint lo exigiria jamas: un permiso fantasma que parece dar acceso.
        var (handler, repository, log) = Build();

        var response = await handler.Handle(
            new SetRolePermissionRequest(RoleId, "access:user:borrar-todo", true, "Motivo suficiente."),
            CancellationToken.None);

        var failure = Assert.IsType<SetRolePermissionValidationFailure>(response);
        Assert.Contains("catalogo", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(repository.Grants);
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task Sin_motivo_no_se_cambia_nada()
    {
        var (handler, repository, log) = Build();

        var response = await handler.Handle(
            new SetRolePermissionRequest(RoleId, PermissionCatalog.AuditLogView, true, "  "),
            CancellationToken.None);

        Assert.IsType<SetRolePermissionValidationFailure>(response);
        Assert.Empty(repository.Grants);
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task Conceder_deja_bitacora_aunque_no_borre_un_solo_dato()
    {
        var (handler, repository, log) = Build();

        var response = await handler.Handle(
            new SetRolePermissionRequest(RoleId, PermissionCatalog.AuditLogView, true, "Cierre de mes."),
            CancellationToken.None);

        var success = Assert.IsType<SetRolePermissionSuccess>(response);
        Assert.True(success.Data.Changed);
        Assert.Equal((5L, 9L, true), Assert.Single(repository.Grants));

        var record = Assert.Single(log.Records);
        Assert.Equal(PermissionCatalog.RolesManage, record.ActionCode);
        Assert.Equal("role-permission", record.SubjectType);

        // Llave compuesta, pero llave: identifica la concesion exacta y no depende de como se
        // llame el rol hoy.
        Assert.Equal($"{RoleId}:{PermissionCatalog.AuditLogView}", record.SubjectKey);
        Assert.Contains("concedido", record.SubjectLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Revocar_tambien_queda_registrado()
    {
        var (handler, _, log) = Build();

        await handler.Handle(
            new SetRolePermissionRequest(RoleId, PermissionCatalog.AuditLogView, false, "Revision de riesgos."),
            CancellationToken.None);

        Assert.Contains("revocado", log.Records[0].SubjectLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Conceder_algo_que_ya_estaba_concedido_no_ensucia_la_bitacora()
    {
        // No es un error, pero tampoco es un evento. Registrarlo llenaria la pantalla de
        // renglones que no pasaron y que nadie podria distinguir de los que si.
        var (handler, repository, log) = Build();
        repository.NextSetReturnsChanged = false;

        var response = await handler.Handle(
            new SetRolePermissionRequest(RoleId, PermissionCatalog.AuditLogView, true, "Motivo suficiente."),
            CancellationToken.None);

        var success = Assert.IsType<SetRolePermissionSuccess>(response);
        Assert.False(success.Data.Changed);
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task Un_rol_de_otro_tenant_no_existe()
    {
        var (handler, _, log) = Build();

        var response = await handler.Handle(
            new SetRolePermissionRequest(Guid.NewGuid(), PermissionCatalog.AuditLogView, true, "Motivo suficiente."),
            CancellationToken.None);

        Assert.IsType<SetRolePermissionNotFoundFailure>(response);
        Assert.Empty(log.Records);
    }
}
