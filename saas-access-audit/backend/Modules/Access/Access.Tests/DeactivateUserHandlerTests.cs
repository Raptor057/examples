using Access.Application.UseCases.DeactivateUser;
using Access.Application.UseCases.DeactivateUser.Responses;
using Access.Contracts;
using Access.Domain.Entities;

namespace Access.Tests;

/// <summary>
/// EL CRUCE DE LOS DOS PATRONES, PROBADO.
///
/// No basta con que la accion funcione: tiene que DEJAR RASTRO, con motivo, con la llave de lo
/// afectado y con el estado del efecto externo. Cada una de esas cosas es una prueba aparte
/// porque cada una se puede romper por separado sin que el endpoint devuelva un error.
/// </summary>
public sealed class DeactivateUserHandlerTests
{
    private static AppUser ActiveUser() => new()
    {
        Id = 10,
        PublicId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Username = "dario.luna",
        DisplayName = "Dario Luna",
        Email = "dario.luna@acme.example",
        IsActive = true
    };

    private static (DeactivateUserHandler Handler, FakeAccessWriteRepository Repository, FakeActionAuditLog Log, FakeIdentityProviderGateway Gateway)
        Build(AppUser? user, bool externalEffectApplied = true)
    {
        var repository = new FakeAccessWriteRepository { User = user };
        var log = new FakeActionAuditLog();
        var gateway = new FakeIdentityProviderGateway(externalEffectApplied, "El proveedor respondio 503.");
        return (new DeactivateUserHandler(repository, gateway, log), repository, log, gateway);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ok")]
    public async Task Sin_motivo_no_se_desactiva_a_nadie(string? reason)
    {
        var user = ActiveUser();
        var (handler, repository, log, gateway) = Build(user);

        var response = await handler.Handle(new DeactivateUserRequest(user.PublicId, reason), CancellationToken.None);

        Assert.IsType<DeactivateUserValidationFailure>(response);

        // Y no se toco nada: ni la fila, ni el sistema externo, ni la bitacora.
        Assert.Equal(0, repository.DeactivateCalls);
        Assert.Equal(0, gateway.Calls);
        Assert.Empty(log.Records);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Un_usuario_de_otro_tenant_no_existe()
    {
        // El repositorio real consulta por EF, con el global query filter puesto: pedir el
        // identificador de alguien de otra empresa devuelve "no existe", no sus datos.
        var (handler, _, log, _) = Build(user: null);

        var response = await handler.Handle(
            new DeactivateUserRequest(Guid.NewGuid(), "Motivo suficiente."), CancellationToken.None);

        Assert.IsType<DeactivateUserNotFoundFailure>(response);
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task El_segundo_clic_no_ejecuta_la_accion_dos_veces()
    {
        var user = ActiveUser();
        user.IsActive = false;
        var (handler, _, log, gateway) = Build(user);

        var response = await handler.Handle(
            new DeactivateUserRequest(user.PublicId, "Motivo suficiente."), CancellationToken.None);

        Assert.IsType<DeactivateUserConflictFailure>(response);

        // Lo importante: no se vuelve a llamar al sistema externo ni se agrega un renglon nuevo
        // que despues nadie sabria explicar.
        Assert.Equal(0, gateway.Calls);
        Assert.Empty(log.Records);
    }

    [Fact]
    public async Task La_accion_deja_bitacora_con_el_motivo_y_la_llave()
    {
        var user = ActiveUser();
        var (handler, _, log, _) = Build(user);

        var response = await handler.Handle(
            new DeactivateUserRequest(user.PublicId, "  Baja voluntaria confirmada.  "), CancellationToken.None);

        var success = Assert.IsType<DeactivateUserSuccess>(response);
        Assert.True(success.Data.ExternalEffectApplied);

        var record = Assert.Single(log.Records);
        Assert.Equal(PermissionCatalog.UsersDeactivate, record.ActionCode);
        Assert.Equal("user", record.SubjectType);

        // LA LLAVE, no la descripcion: el dia que alguien renombre al usuario, el renglon sigue
        // encontrandose.
        Assert.Equal(user.PublicId.ToString(), record.SubjectKey);
        Assert.Equal("dario.luna", record.SubjectLabel);

        // El motivo llega recortado, no tal cual lo escribio el navegador.
        Assert.Equal("Baja voluntaria confirmada.", record.Reason);

        // Nadie autorizo por excepcion, asi que la columna se queda vacia en vez de repetir a
        // quien ejecuto.
        Assert.Null(record.AuthorizedBy);
    }

    [Fact]
    public async Task El_codigo_de_accion_es_el_del_permiso_que_la_autorizo()
    {
        // Es el detalle que permite cruzar las dos bitacoras: para un mismo codigo se puede ver
        // quien lo hizo y quien lo intento sin poder.
        var user = ActiveUser();
        var (handler, _, log, _) = Build(user);

        await handler.Handle(new DeactivateUserRequest(user.PublicId, "Motivo suficiente."), CancellationToken.None);

        Assert.Equal(PermissionCatalog.UsersDeactivate, log.Records[0].ActionCode);
        Assert.True(PermissionCatalog.Contains(log.Records[0].ActionCode));
    }

    [Fact]
    public async Task Si_el_efecto_externo_falla_la_desactivacion_NO_se_revierte()
    {
        var user = ActiveUser();
        var (handler, _, log, gateway) = Build(user, externalEffectApplied: false);

        var response = await handler.Handle(
            new DeactivateUserRequest(user.PublicId, "Incidente de seguridad."), CancellationToken.None);

        // Sigue siendo exito: la desactivacion ocurrio. Revertirla dejaria al usuario activo sin
        // que nadie se entere, que es peor que un efecto externo pendiente y avisado.
        var success = Assert.IsType<DeactivateUserSuccess>(response);
        Assert.False(user.IsActive);
        Assert.Equal(1, gateway.Calls);

        // Y se REPORTA HACIA ARRIBA, para que la pantalla lo pinte como advertencia.
        Assert.False(success.Data.ExternalEffectApplied);
        Assert.NotNull(success.Data.ExternalEffectError);

        // La bitacora es la unica forma de encontrar despues los registros descuadrados.
        var record = Assert.Single(log.Records);
        Assert.Equal("identity-provider:revoke-sessions", record.ExternalEffectName);
        Assert.False(record.ExternalEffectApplied);
        Assert.NotNull(record.ExternalEffectError);
    }

    [Fact]
    public async Task La_bitacora_se_escribe_DESPUES_del_efecto_externo()
    {
        // El orden importa: la bitacora tiene que registrar COMO termino el efecto externo, y por
        // eso no puede compartir transaccion con la escritura ni ejecutarse antes.
        var user = ActiveUser();
        var (handler, _, log, gateway) = Build(user, externalEffectApplied: false);

        await handler.Handle(new DeactivateUserRequest(user.PublicId, "Motivo suficiente."), CancellationToken.None);

        Assert.Equal(1, gateway.Calls);
        Assert.False(log.Records[0].ExternalEffectApplied);
    }
}
