using System.Diagnostics;
using System.Globalization;
using LazyHierarchy.Kernel;
using LazyHierarchy.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Seeding;

/// <summary>
/// Siembra del ejemplo. El volumen no es decorado: sin un par de millones de lineas, un arbol
/// perezoso y un arbol que trae todo se ven igual de rapidos y el ejemplo no demuestra nada.
///
/// Va por EF Core, como toda escritura del proyecto, con tres ajustes que hacen la diferencia
/// entre minutos y horas:
///   - deteccion de cambios apagada (el ChangeTracker no tiene que recorrer 10 mil entidades
///     en cada Add),
///   - un DbContext NUEVO por lote (el tracker no crece sin limite),
///   - lotes grandes, que Npgsql agrupa en pocos viajes a la base.
/// </summary>
public sealed class OrdersSeeder(
    IServiceScopeFactory scopeFactory,
    ITenantContextAccessor tenantAccessor,
    IConfiguration configuration,
    ILogger<OrdersSeeder> logger)
{
    private static readonly string[] CategoryNames =
    [
        "Audio", "Computo", "Fotografia", "Hogar inteligente", "Juguetes",
        "Libros", "Moda", "Oficina", "Salud", "Videojuegos"
    ];

    private static readonly string[] ProductWords =
    [
        "Aurora", "Basalto", "Cobalto", "Delta", "Estrato", "Fenix", "Granito", "Helio",
        "Indigo", "Jade", "Kelvin", "Lumen", "Meridiano", "Nimbo", "Onix", "Pulsar"
    ];

    private static readonly string[] CustomerNames =
    [
        "Alvarez y Asociados", "Bazar del Norte", "Comercial Poniente", "Distribuidora Sol",
        "Empaques del Golfo", "Ferreteria Central", "Grupo Marfil", "Hidalgo Retail",
        "Importadora Lince", "Juguetes del Valle", "Kiosco Digital", "Logistica Zafiro"
    ];

    private static readonly string[] Statuses = ["placed", "picked", "shipped", "delivered", "cancelled"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var targetLines = configuration.GetValue("Seed:TargetOrderLines", 1_500_000);
        var batchOrders = configuration.GetValue("Seed:BatchOrders", 2_000);
        var startYear = configuration.GetValue("Seed:StartYear", DateTime.UtcNow.Year - 3);
        var years = configuration.GetValue("Seed:Years", 4);
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

        // Reparto entre tenants. Dos tenants con volumenes distintos: asi se nota que el arbol
        // de uno no ve ni un renglon del otro, y que el conteo de cada nodo tampoco lo delata.
        var plans = new[]
        {
            new TenantPlan("acme", "ACME Retail", (long)(targetLines * 0.65)),
            new TenantPlan("globex", "Globex Comercial", targetLines - (long)(targetLines * 0.65))
        };

        var tenantIds = await CreateTenantsAsync(plans, cancellationToken).ConfigureAwait(false);

        foreach (var plan in plans)
        {
            var tenantId = tenantIds[plan.Code];

            // El seeder opera COMO el tenant: establece el contexto en vez de saltarse los
            // filtros. Asi la siembra recorre exactamente el mismo camino que la aplicacion.
            tenantAccessor.Current = new TenantContext(tenantId);
            try
            {
                await SeedTenantAsync(plan, tenantId, startYear, years, batchOrders, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                tenantAccessor.Current = null;
            }
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Seed terminado en {Elapsed}. Objetivo: {TargetLines} lineas de pedido.",
            stopwatch.Elapsed, targetLines);
    }

    private async Task<Dictionary<string, long>> CreateTenantsAsync(TenantPlan[] plans, CancellationToken cancellationToken)
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

    private async Task SeedTenantAsync(
        TenantPlan plan, long tenantId, int startYear, int years, int batchOrders, CancellationToken cancellationToken)
    {
        // Semilla fija por tenant: dos corridas del seeder producen los mismos datos, y eso hace
        // que los numeros del README se puedan reproducir.
        var random = new Random(plan.Code.GetHashCode(StringComparison.Ordinal));

        var (categoryIds, productIds, productPrices) =
            await SeedCatalogAsync(plan, tenantId, random, cancellationToken).ConfigureAwait(false);

        const int minLines = 1;
        const int maxLines = 6;
        var averageLines = (minLines + maxLines) / 2.0;
        var totalOrders = (int)Math.Max(1, plan.TargetLines / averageLines);

        var periodStart = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodDays = (new DateTime(startYear + years, 1, 1, 0, 0, 0, DateTimeKind.Utc) - periodStart).TotalDays;

        var stopwatch = Stopwatch.StartNew();
        var createdOrders = 0;
        var createdLines = 0L;
        var orderSequence = 0;

        while (createdOrders < totalOrders)
        {
            var take = Math.Min(batchOrders, totalOrders - createdOrders);

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var batch = new List<CustomerOrder>(take);
            for (var i = 0; i < take; i++)
            {
                orderSequence++;
                var placedAt = periodStart.AddSeconds(random.NextDouble() * periodDays * 86_400d);
                var order = new CustomerOrder
                {
                    TenantId = tenantId,
                    OrderNumber = string.Create(CultureInfo.InvariantCulture, $"{plan.Code.ToUpperInvariant()}-{orderSequence:D8}"),
                    CustomerName = CustomerNames[random.Next(CustomerNames.Length)],
                    Status = Statuses[random.Next(Statuses.Length)],
                    PlacedAtUtc = placedAt
                };

                var lineCount = random.Next(minLines, maxLines + 1);
                decimal total = 0m;
                var usedProducts = new HashSet<long>(lineCount);
                for (var line = 0; line < lineCount; line++)
                {
                    var productIndex = random.Next(productIds.Count);
                    if (!usedProducts.Add(productIds[productIndex])) continue;

                    var quantity = random.Next(1, 13);
                    var unitPrice = productPrices[productIndex];
                    var lineTotal = decimal.Round(unitPrice * quantity, 2);
                    total += lineTotal;

                    order.Lines.Add(new OrderLine
                    {
                        TenantId = tenantId,
                        ProductId = productIds[productIndex],
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        LineTotal = lineTotal
                    });
                }

                order.TotalAmount = total;
                createdLines += order.Lines.Count;
                batch.Add(order);
            }

            db.Set<CustomerOrder>().AddRange(batch);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            createdOrders += take;
            var rate = createdLines / Math.Max(1d, stopwatch.Elapsed.TotalSeconds);
            logger.LogInformation(
                "[{Tenant}] {Orders}/{TotalOrders} pedidos ({Percent:P1}) | {Lines} lineas | {Rate:N0} lineas/s | transcurrido {Elapsed:hh\\:mm\\:ss}",
                plan.Code, createdOrders, totalOrders, (double)createdOrders / totalOrders, createdLines, rate, stopwatch.Elapsed);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "[{Tenant}] listo: {Orders} pedidos, {Lines} lineas, {Categories} categorias, {Products} productos en {Elapsed:hh\\:mm\\:ss}.",
            plan.Code, createdOrders, createdLines, categoryIds.Count, productIds.Count, stopwatch.Elapsed);
    }

    private async Task<(List<long> CategoryIds, List<long> ProductIds, List<decimal> ProductPrices)> SeedCatalogAsync(
        TenantPlan plan, long tenantId, Random random, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categories = new List<Category>(CategoryNames.Length);
        for (var i = 0; i < CategoryNames.Length; i++)
        {
            categories.Add(new Category
            {
                TenantId = tenantId,
                Code = string.Create(CultureInfo.InvariantCulture, $"CAT-{i + 1:D2}"),
                Name = CategoryNames[i]
            });
        }

        db.Set<Category>().AddRange(categories);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var products = new List<Product>();
        var sku = 0;
        foreach (var category in categories)
        {
            for (var i = 0; i < 30; i++)
            {
                sku++;
                products.Add(new Product
                {
                    TenantId = tenantId,
                    CategoryId = category.Id,
                    Sku = string.Create(CultureInfo.InvariantCulture, $"{plan.Code.ToUpperInvariant()}-SKU-{sku:D5}"),
                    Name = string.Create(CultureInfo.InvariantCulture,
                        $"{ProductWords[random.Next(ProductWords.Length)]} {category.Name} {i + 1:D2}"),
                    UnitPrice = decimal.Round((decimal)(random.NextDouble() * 900d + 20d), 2)
                });
            }
        }

        db.Set<Product>().AddRange(products);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[{Tenant}] catalogo listo: {Categories} categorias y {Products} productos.",
            plan.Code, categories.Count, products.Count);

        return (
            categories.Select(category => category.Id).ToList(),
            products.Select(product => product.Id).ToList(),
            products.Select(product => product.UnitPrice).ToList());
    }

    private sealed record TenantPlan(string Code, string Name, long TargetLines);
}
