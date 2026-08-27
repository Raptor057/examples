using System.Reflection;
using Access.Contracts;
using Audit.Application.UseCases.GetAccessDenials;
using Audit.Application.UseCases.GetAccessDenials.Responses;
using Audit.Application.UseCases.GetActionAudit;
using Audit.Application.UseCases.GetActionAudit.Responses;
using Audit.Domain.Models;
using Audit.Domain.Repositories;
using Audit.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using SaasAccessAudit.Kernel;
using SaasAccessAudit.Shared.Web.Authorization;

namespace Audit.Tests;

internal sealed class FakeAuditReadRepository : IAuditReadRepository
{
    public ActionAuditFilter? LastActionFilter { get; private set; }

    public AccessDenialFilter? LastDenialFilter { get; private set; }

    public int LastPageSize { get; private set; }

    public Task<PagedResult<ActionAuditItem>> GetActionsPageAsync(
        ActionAuditFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        LastActionFilter = filter;
        LastPageSize = pageSize;
        return Task.FromResult(PagedResult<ActionAuditItem>.Empty(pageNumber, pageSize));
    }

    public Task<PagedResult<AccessDenialItem>> GetDenialsPageAsync(
        AccessDenialFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        LastDenialFilter = filter;
        LastPageSize = pageSize;
        return Task.FromResult(PagedResult<AccessDenialItem>.Empty(pageNumber, pageSize));
    }
}

/// <summary>
/// La pantalla de consulta: que normaliza antes de tocar la base, y que politica la protege.
/// </summary>
public sealed class AuditHandlerTests
{
    [Fact]
    public async Task El_tamano_de_pagina_se_topa_antes_de_llegar_a_la_base()
    {
        var repository = new FakeAuditReadRepository();
        var handler = new GetActionAuditHandler(repository);

        await handler.Handle(
            new GetActionAuditRequest(null, null, null, null, null, false, 1, 50_000), CancellationToken.None);

        Assert.Equal(Paging.MaxPageSize, repository.LastPageSize);
    }

    [Fact]
    public async Task Un_rango_invertido_se_rechaza_en_vez_de_devolver_vacio()
    {
        // La pantalla vacia que nadie sabe explicar: sin esta validacion, el usuario ve cero
        // renglones y concluye que no paso nada, cuando lo que pasa es que puso las fechas al
        // reves.
        var handler = new GetActionAuditHandler(new FakeAuditReadRepository());

        var response = await handler.Handle(
            new GetActionAuditRequest(
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                null, null, null, false, null, null),
            CancellationToken.None);

        Assert.IsType<GetActionAuditValidationFailure>(response);
    }

    [Fact]
    public async Task Los_filtros_en_blanco_no_filtran()
    {
        // Un filtro con espacios tiene que comportarse como "sin filtrar", no como "busca la
        // cadena vacia": lo segundo devuelve cero renglones sin decir por que.
        var repository = new FakeAuditReadRepository();
        var handler = new GetActionAuditHandler(repository);

        await handler.Handle(
            new GetActionAuditRequest(null, null, "   ", "", null, false, null, null), CancellationToken.None);

        Assert.Null(repository.LastActionFilter!.ActionCode);
        Assert.Null(repository.LastActionFilter.SubjectKey);
        Assert.False(repository.LastActionFilter.Shape.HasActionCode);
    }

    [Fact]
    public async Task Los_filtros_llegan_recortados()
    {
        var repository = new FakeAuditReadRepository();
        var handler = new GetActionAuditHandler(repository);

        await handler.Handle(
            new GetActionAuditRequest(null, null, "  access:user:deactivate  ", null, " ana.torres ", false, null, null),
            CancellationToken.None);

        Assert.Equal(PermissionCatalog.UsersDeactivate, repository.LastActionFilter!.ActionCode);
        Assert.Equal("ana.torres", repository.LastActionFilter.PerformedBy);
    }

    [Fact]
    public async Task El_filtro_de_lo_que_se_habria_bloqueado_llega_a_la_consulta()
    {
        var repository = new FakeAuditReadRepository();
        var handler = new GetAccessDenialsHandler(repository);

        await handler.Handle(
            new GetAccessDenialsRequest(null, null, null, null, true, null, null), CancellationToken.None);

        Assert.True(repository.LastDenialFilter!.Shape.OnlyWouldHaveBeenBlocked);
    }

    [Fact]
    public async Task La_pantalla_responde_con_el_total_y_la_pagina()
    {
        var handler = new GetAccessDenialsHandler(new FakeAuditReadRepository());

        var response = await handler.Handle(
            new GetAccessDenialsRequest(null, null, null, null, false, 3, 10), CancellationToken.None);

        var success = Assert.IsType<GetAccessDenialsSuccess>(response);
        Assert.Equal(3, success.Data.PageNumber);
        Assert.Equal(10, success.Data.PageSize);
    }

    [Fact]
    public void Consultar_la_bitacora_lleva_permiso_propio()
    {
        // Registra quien hizo que, y no todos deben verlo. El permiso sale del catalogo del
        // producto, que vive en Access.Contracts: la unica via legitima entre modulos.
        var policy = typeof(AuditController).GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(policy);
        Assert.Equal(PermissionCatalog.AuditLogView, policy.PermissionCode);
        Assert.True(PermissionCatalog.Contains(policy.PermissionCode));
        Assert.NotNull(typeof(AuditController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void La_bitacora_solo_expone_lecturas()
    {
        // No hay endpoint para editar ni para borrar un renglon, y no es que falte: una bitacora
        // que se puede editar no es una bitacora.
        var writeEndpoints = typeof(AuditController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>())
            .SelectMany(attribute => attribute.HttpMethods)
            .Where(verb => verb is not "GET")
            .ToList();

        Assert.Empty(writeEndpoints);
    }
}
