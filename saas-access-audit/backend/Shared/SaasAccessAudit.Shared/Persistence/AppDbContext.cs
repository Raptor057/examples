using System.Reflection;
using SaasAccessAudit.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SaasAccessAudit.Shared.Persistence;

/// <summary>
/// Los ensamblados de los que se toman las IEntityTypeConfiguration. Vive aqui, en la capa de
/// datos compartida, para que el DbContext sea uno solo para todo el sistema y cada modulo
/// aporte sus configuraciones sin que el DbContext conozca los modulos.
/// </summary>
public sealed class ModelConfigurationSources(params Assembly[] assemblies)
{
    public IReadOnlyList<Assembly> Assemblies { get; } = assemblies;
}

/// <summary>
/// DbContext unico del sistema. No declara DbSets por entidad: los modulos aportan sus
/// entidades via configuraciones y los repositorios trabajan con Set&lt;T&gt;().
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantContextAccessor tenantAccessor,
    ModelConfigurationSources configurationSources)
    : DbContext(options)
{
    // Se lee en CADA consulta: sin contexto vale el contexto sistema y el filtro no devuelve
    // filas de negocio. Fail-closed.
    private long CurrentTenantId => tenantAccessor.Current?.TenantId ?? SystemContext.TenantId;

    /// <summary>
    /// El mismo valor, para el unico caso en que un repositorio tiene que escribir el filtro a
    /// mano: cuando usa IgnoreQueryFilters() para alcanzar una fila dada de baja.
    ///
    /// IgnoreQueryFilters quita TODOS los filtros de golpe, no solo el de activos, asi que quien
    /// lo use debe reponer el de tenant en el mismo Where. Se expone aqui -y no se deja que cada
    /// repositorio se busque el accessor- para que ese valor salga siempre del mismo sitio que el
    /// filtro automatico, y no de otro que algun dia diga otra cosa.
    /// </summary>
    public long CurrentTenantIdForExplicitFilters => CurrentTenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var assembly in configurationSources.Assemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        ApplyKernelConventions(modelBuilder);
        ApplySnakeCaseNaming(modelBuilder);
    }

    /// <summary>
    /// El chasis: PublicId, indice unico y global query filter segun la interfaz que declara
    /// cada entidad. Aqui esta la razon de que en este proyecto no exista un HasQueryFilter por
    /// entidad que alguien pueda olvidar al agregar una tabla.
    /// </summary>
    private void ApplyKernelConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (entityType.IsOwned()) continue;

            if (typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                var entity = modelBuilder.Entity(clrType);
                entity.Property(nameof(BaseEntity.PublicId)).HasDefaultValueSql("gen_random_uuid()");
                entity.HasIndex(nameof(BaseEntity.PublicId)).IsUnique();
            }

            var isTenantOwned = typeof(ITenantOwned).IsAssignableFrom(clrType);
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(clrType);
            if (!isTenantOwned && !isSoftDeletable) continue;

            var methodName = (isTenantOwned, isSoftDeletable) switch
            {
                (true, true) => nameof(FilterTenantOwnedSoftDeletable),
                (true, false) => nameof(FilterTenantOwned),
                _ => nameof(FilterSoftDeletable)
            };

            typeof(AppDbContext)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(clrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void FilterTenantOwnedSoftDeletable<T>(ModelBuilder modelBuilder) where T : class, ITenantOwned, ISoftDeletable =>
        modelBuilder.Entity<T>().HasQueryFilter(e => e.IsActive && e.TenantId == CurrentTenantId);

    private void FilterTenantOwned<T>(ModelBuilder modelBuilder) where T : class, ITenantOwned =>
        modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == CurrentTenantId);

    private void FilterSoftDeletable<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable =>
        modelBuilder.Entity<T>().HasQueryFilter(e => e.IsActive);

    /// <summary>
    /// Tablas, columnas, llaves e indices en snake_case sin comillas. Los indices unicos llevan
    /// prefijo ux_ y los demas ix_, para que el nombre diga de un vistazo que garantiza cada uno.
    /// </summary>
    private static void ApplySnakeCaseNaming(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName is not null)
                entityType.SetTableName(SnakeCaseNaming.ToSnakeCase(tableName));

            foreach (var property in entityType.GetProperties())
                property.SetColumnName(SnakeCaseNaming.ToSnakeCase(property.GetColumnName()));

            foreach (var key in entityType.GetKeys())
                key.SetName(SnakeCaseNaming.ToSnakeCase(key.GetName() ?? string.Empty));

            foreach (var foreignKey in entityType.GetForeignKeys())
                foreignKey.SetConstraintName(SnakeCaseNaming.ToSnakeCase(foreignKey.GetConstraintName() ?? string.Empty));

            foreach (var index in entityType.GetIndexes())
            {
                var name = SnakeCaseNaming.ToSnakeCase(index.GetDatabaseName() ?? string.Empty);
                if (index.IsUnique && name.StartsWith("ix_", StringComparison.Ordinal))
                    name = string.Concat("ux_", name.AsSpan(3));
                index.SetDatabaseName(name);
            }
        }
    }
}
