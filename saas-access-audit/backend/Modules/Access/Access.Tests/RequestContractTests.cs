using System.Reflection;
using Common.Messaging;

namespace Access.Tests;

/// <summary>
/// LA IDENTIDAD NO LLEGA DEL CLIENTE, Y ESTA PRUEBA LO VIGILA.
///
/// Quien eres y de que empresa eres salen del token. Si un Request tuviera un campo de usuario o
/// de tenant, cualquiera podria leer -o firmar- lo de otro cambiando un valor en la peticion, y
/// el sintoma no seria un error: seria una bitacora que acusa a la persona equivocada.
///
/// Es una prueba de arquitectura y por eso mira los TIPOS, no el comportamiento: el dia que
/// alguien agregue ese campo por comodidad, la prueba se lo dice antes que un incidente.
/// </summary>
public sealed class RequestContractTests
{
    private static readonly string[] ForbiddenFragments =
    [
        "tenant", "userid", "username", "performedby", "actor", "currentuser"
    ];

    public static TheoryData<string> RequestTypes()
    {
        var data = new TheoryData<string>();
        foreach (var type in AllRequestTypes()) data.Add(type.FullName!);
        return data;
    }

    private static IEnumerable<Type> AllRequestTypes() =>
        typeof(Access.Application.ServiceCollectionEx).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)));

    [Fact]
    public void Hay_requests_que_revisar()
    {
        // Un guardia que no encuentra nada que vigilar pasa en verde sin comprobar nada.
        Assert.NotEmpty(AllRequestTypes());
    }

    [Theory]
    [MemberData(nameof(RequestTypes))]
    public void Ningun_request_recibe_la_identidad_del_cliente(string typeName)
    {
        var type = AllRequestTypes().Single(item => item.FullName == typeName);

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = property.Name.ToLowerInvariant();

            // UserPublicId es el SUJETO de la accion (a quien se la haces), no quien la hace.
            if (name is "userpublicid" or "rolepublicid") continue;

            foreach (var fragment in ForbiddenFragments)
            {
                Assert.False(
                    name.Contains(fragment, StringComparison.Ordinal),
                    $"{typeName}.{property.Name} parece traer la identidad en la peticion. "
                    + "El usuario y el tenant salen del token, nunca del cliente.");
            }
        }
    }

    [Fact]
    public void La_deteccion_encuentra_un_request_contaminado()
    {
        // Prueba de la prueba: si el reconocedor dejara de encontrar nombres prohibidos, todas
        // las demas pasarian en verde sin comprobar nada.
        var contaminated = new[] { "TenantId", "PerformedBy", "CurrentUser" };

        foreach (var name in contaminated)
        {
            Assert.Contains(
                ForbiddenFragments,
                fragment => name.ToLowerInvariant().Contains(fragment, StringComparison.Ordinal));
        }
    }
}
