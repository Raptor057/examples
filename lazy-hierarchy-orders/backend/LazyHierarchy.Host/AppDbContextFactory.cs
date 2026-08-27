using LazyHierarchy.Shared.Persistence;
using LazyHierarchy.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LazyHierarchy.Host;

/// <summary>
/// Factory de tiempo de diseno: la usa "dotnet ef migrations add". El DbContext real recibe el
/// accessor de tenant y los ensamblados de configuracion por inyeccion, y las herramientas de EF
/// no saben construirlo solas.
///
/// Corre bajo el contexto sistema (accessor vacio): una migracion cambia el esquema, no consulta
/// datos de negocio, asi que los filtros no le estorban.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MainDb")
            ?? "Host=localhost;Port=55432;Database=lazy_hierarchy;Username=lazy;Password=lazy_dev_password";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly("Orders.Infrastructure"))
            .Options;

        return new AppDbContext(
            options,
            new TenantContextAccessor(),
            new ModelConfigurationSources(Orders.Infrastructure.ServiceCollectionEx.ModelConfigurationAssembly));
    }
}
