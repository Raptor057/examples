using LazyHierarchy.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Infrastructure.Repositories;

/// <summary>
/// El lado EF del ejemplo: todas las escrituras.
///
/// Ni un solo Where por tenant en este archivo. El global query filter lo pone en cada lectura
/// y el interceptor sella el tenant en cada insert, los dos desde el contexto de la peticion.
/// Ese contraste con el repositorio de lectura es la leccion: donde hay aislamiento que olvidar,
/// lo aplica el ORM.
/// </summary>
internal sealed class OrderWriteRepository(AppDbContext db) : IOrderWriteRepository
{
    public Task<Product?> GetProductAsync(Guid productPublicId, CancellationToken cancellationToken = default) =>
        db.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.PublicId == productPublicId, cancellationToken);

    public Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        db.Set<CustomerOrder>()
            .AsNoTracking()
            .AnyAsync(order => order.OrderNumber == orderNumber, cancellationToken);

    public async Task<Guid> CreateAsync(CustomerOrder order, CancellationToken cancellationToken = default)
    {
        // Un solo SaveChangesAsync ya es atomico para el pedido y sus lineas: no hace falta
        // transaccion explicita.
        db.Set<CustomerOrder>().Add(order);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return order.PublicId;
    }
}
