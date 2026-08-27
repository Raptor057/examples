using System.Reflection;
using Access.Contracts;
using Access.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SaasAccessAudit.Shared.Web.Authorization;

namespace Access.Tests;

/// <summary>
/// LA PRUEBA QUE ATRAPA EL 403 IMPOSIBLE DE DIAGNOSTICAR.
///
/// El punto 6 de la lista de diagnostico de un rechazo dice: "el codigo en el endpoint coincide
/// exactamente con el del catalogo? Un typo aqui se ve igual que no tienes permiso". Esta prueba
/// recorre los endpoints del modulo y falla si alguno exige un permiso que el catalogo no
/// declara, con lo que ese typo deja de poder llegar a produccion.
/// </summary>
public sealed class EndpointPolicyTests
{
    private static IEnumerable<MethodInfo> Endpoints() =>
        typeof(AccessController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any());

    public static TheoryData<string> EndpointNames()
    {
        var data = new TheoryData<string>();
        foreach (var endpoint in Endpoints()) data.Add(endpoint.Name);
        return data;
    }

    [Theory]
    [MemberData(nameof(EndpointNames))]
    public void Todo_permiso_exigido_existe_en_el_catalogo(string endpointName)
    {
        var endpoint = Endpoints().Single(method => method.Name == endpointName);
        var policy = endpoint.GetCustomAttribute<HasPermissionAttribute>();

        if (policy is null) return;

        Assert.True(
            PermissionCatalog.Contains(policy.PermissionCode),
            $"El endpoint {endpointName} exige el permiso '{policy.PermissionCode}', que no esta en el catalogo. "
            + "Declaralo en PermissionCatalog o corrige el codigo: un typo aqui se ve igual que un permiso faltante.");
    }

    [Fact]
    public void El_controller_exige_autenticacion()
    {
        // Sin token no hay claim de tenant, y sin tenant el filtro compara contra el contexto
        // sistema. El candado y el aislamiento son el mismo hilo.
        Assert.NotNull(typeof(AccessController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Toda_escritura_declara_su_politica()
    {
        // Una lectura puede ser publica por diseno; una ESCRITURA sin politica es un hueco.
        foreach (var endpoint in Endpoints())
        {
            var isWrite = endpoint.GetCustomAttributes<HttpMethodAttribute>()
                .SelectMany(attribute => attribute.HttpMethods)
                .Any(verb => verb is "POST" or "PUT" or "PATCH" or "DELETE");

            if (!isWrite) continue;

            Assert.True(
                endpoint.GetCustomAttribute<HasPermissionAttribute>() is not null,
                $"El endpoint de escritura {endpoint.Name} no declara [HasPermission].");
        }
    }

    [Fact]
    public void La_accion_destructiva_no_reusa_el_permiso_de_lectura()
    {
        var deactivate = Endpoints().Single(method => method.Name == nameof(AccessController.DeactivateUser));
        var policy = deactivate.GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(policy);
        Assert.Equal(PermissionCatalog.UsersDeactivate, policy.PermissionCode);
        Assert.NotEqual(PermissionCatalog.UsersView, policy.PermissionCode);
    }

    [Fact]
    public void Consultar_lo_propio_no_pide_permiso()
    {
        // La bandeja, las preferencias y el perfil de uno mismo no piden permiso: todos los
        // tienen. Pedir uno aqui dejaria a la aplicacion sin forma de saber que esconder.
        var me = Endpoints().Single(method => method.Name == nameof(AccessController.GetMyAccess));

        Assert.Null(me.GetCustomAttribute<HasPermissionAttribute>());
    }
}
