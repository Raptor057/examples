# 05 - Catalogo de modulos

Descripcion de cada modulo del proyecto: que hace, que contrato expone (si expone
alguno), quien lo consume, que capas tiene y que endpoints aporta.

Hay dos grupos: globales (transversales) y de negocio.

## Resumen

| Modulo | Grupo | Contrato que expone | Lo consume | Endpoint |
|--------|-------|---------------------|------------|----------|
| Identity | global | `ICurrentUserAccessor` | Ventas | (ninguno propio) |
| Geo | global | `IGeoCatalog` | Ventas | `GET /api/geo/paises` |
| Inventario | negocio | `IConsultaInventario` | Ventas | `GET /api/inventario/productos` |
| Ventas | negocio | (ninguno) | (nadie) | `POST /api/ventas`, `GET /api/ventas` |

---

## Global: Identity

Responsabilidad: saber quien esta haciendo la peticion (el usuario actual). Es el
global mas universal; en un sistema real todos los modulos dependen de el.

Contrato (`Identity.Contracts/ICurrentUserAccessor.cs`):

```csharp
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string Nombre { get; }
    string Rol { get; }
}
```

Implementacion (`Identity.Infrastructure/CurrentUserAccessor.cs`): lee los datos del
contexto HTTP. En este ejemplo, de headers (`X-User-Id`, `X-User-Name`,
`X-User-Role`); si no vienen, usa valores demo ("Usuario Demo", "Vendedor"). En
un sistema real, en cambio, lee los claims del JWT.

Capas: solo `Contracts` + `Infrastructure`. No necesita Domain, Application ni
Presentation porque es un accessor: no tiene entidades propias ni endpoints, solo
traduce el contexto del request a datos.

Registro (`Identity.Infrastructure/ServiceCollectionEx.cs`):

```csharp
services.AddHttpContextAccessor();
services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
```

Quien lo consume: `Ventas`, para registrar quien hizo la venta.

---

## Global: Geo

Responsabilidad: catalogo geografico (paises). Sirve para validar o listar. Es el
ejemplo de un global tipo "catalogo con datos".

Contrato (`Geo.Contracts/IGeoCatalog.cs`):

```csharp
public sealed record PaisInfo(string Codigo, string Nombre);

public interface IGeoCatalog
{
    Task<IReadOnlyList<PaisInfo>> ListarPaisesAsync(CancellationToken ct = default);
    Task<bool> ExistePaisAsync(string codigo, CancellationToken ct = default);
}
```

Capas: stack completo, porque tiene datos y endpoint propio.

- `Geo.Domain`: entidad `Pais` + `IPaisRepository`.
- `Geo.Application`: `GeoCatalogService` (implementa el contrato) + el use case
  `ListarPaises` (Request/Response/Success/Handler) para el endpoint.
- `Geo.Infrastructure`: `InMemoryPaisRepository` con seed MX, US, CA.
- `Geo.Presentation`: `GeoController` con `GET /api/geo/paises` + su presenter.

Quien lo consume: `Ventas`, para validar el `paisCliente` antes de registrar.

Endpoint propio: `GET /api/geo/paises` devuelve el catalogo.

---

## Negocio: Inventario (proveedor)

Responsabilidad: dueño de los productos y su stock. Es proveedor: otros modulos le
piden datos y operaciones sobre productos.

Contrato (`Inventario.Contracts/IConsultaInventario.cs`):

```csharp
public sealed record ProductoInfo(long ProductoId, string Nombre, decimal Precio, int StockDisponible);
public sealed record ResultadoAjusteStock(bool Exitoso, string? Error, int StockRestante);

public interface IConsultaInventario
{
    Task<IReadOnlyList<ProductoInfo>> ListarAsync(CancellationToken ct = default);
    Task<ProductoInfo?> ObtenerProductoAsync(long productoId, CancellationToken ct = default);
    Task<ResultadoAjusteStock> DescontarStockAsync(long productoId, int cantidad, CancellationToken ct = default);
}
```

Capas: completas.

- `Inventario.Domain`: entidad `Producto` (con metodo `Descontar`) + `IProductoRepository`.
- `Inventario.Application`: `ConsultaInventarioService` (implementa el contrato) +
  use case `ListarProductos` para el endpoint.
- `Inventario.Infrastructure`: `InMemoryProductoRepository` con seed (3 productos),
  registrado como Singleton para que el stock persista entre peticiones en la demo.
- `Inventario.Presentation`: `InventarioController` con `GET /api/inventario/productos`.

Quien lo consume: `Ventas`, para obtener precio/stock y descontar al vender.

Endpoint propio: `GET /api/inventario/productos` devuelve el catalogo con stock.

Nota sobre el doble uso del contrato: `IConsultaInventario` lo usa tanto otro
modulo (Ventas, cross-module) como el propio modulo. El endpoint propio
`ListarProductos`, en cambio, va por el Mediator como cualquier use case. Asi se ven
los dos caminos en un mismo modulo.

---

## Negocio: Ventas (consumidor)

Responsabilidad: registrar ventas. Es el consumidor estrella: para registrar una
venta necesita Inventario (precio y stock), Geo (validar pais) e Identity (quien
vende).

Capas:

- `Ventas.Domain`: entidad `Venta` (guarda `ProductoId`, `Vendedor`, `PaisCliente`,
  total, fecha) + `IVentaRepository`. Ojo: `Venta` guarda datos planos (un
  `ProductoId` long, un `Vendedor` string); NO conoce las entidades de los otros
  modulos.
- `Ventas.Application`: dos use cases.
  - `RegistrarVenta`: consume los tres contratos. Tiene Request, Response,
    Success y tres Failures (`ProductoNoExiste`, `StockInsuficiente`,
    `PaisInvalido`).
  - `ListarVentas`: devuelve las ventas registradas.
- `Ventas.Infrastructure`: `InMemoryVentaRepository`.
- `Ventas.Presentation`: `VentasController` (`POST /api/ventas`, `GET /api/ventas`)
  + presenters para cada use case.

Contrato que expone: ninguno, porque en este ejemplo nadie consume a Ventas. Si en
el futuro otro modulo necesitara datos de ventas, se crearia `Ventas.Contracts`
con la interfaz correspondiente.

Endpoints:

- `POST /api/ventas` con body `{ productoId, cantidad, paisCliente }` y header
  opcional `X-User-Name`.
- `GET /api/ventas` lista las ventas (incluye vendedor y pais).

---

## Como decidir las capas de un modulo nuevo

- Si solo expone una capacidad sin datos propios ni endpoints (un accessor): basta
  `Contracts` + `Infrastructure` (como Identity).
- Si tiene datos y/o endpoints: stack completo
  (`Domain` + `Application` + `Infrastructure` + `Presentation`, y `Contracts` si
  alguien lo va a consumir).

La receta paso a paso para crear modulos esta en
[06-recetas-y-extension.md](06-recetas-y-extension.md).
