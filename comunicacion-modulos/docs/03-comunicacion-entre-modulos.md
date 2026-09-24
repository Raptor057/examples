# 03 - Comunicacion entre modulos

Este es el tema central del proyecto. Explica las dos maneras en que se comunican
las partes de la aplicacion y por que se hace asi.

## Dos tipos de comunicacion

| Tipo | Cuando | Como | Ejemplo |
|------|--------|------|---------|
| Intra-modulo | dentro de un mismo modulo | Mediator (Request -> Handler -> Presenter) | el controller de Ventas llama a su handler |
| Cross-module | entre modulos distintos | Contratos (`*.Contracts`) + inyeccion de dependencias | Ventas le pide datos a Inventario |

Es importante no confundirlas: el Mediator NO se usa para que un modulo hable con
otro. El Mediator es el flujo interno de un modulo. Entre modulos se usan los
contratos.

## Comunicacion intra-modulo (Mediator)

Dentro de un modulo, el controller no llama directo al handler: envia un Request
por el Mediator, y el Mediator encuentra al handler y, al terminar, publica la
respuesta a los presenters.

```
VentasController
   -> Mediator.Send(new RegistrarVentaRequest(...))
        -> InteractorPipeline (registra logs automaticamente)
             -> RegistrarVentaHandler.Handle(...)  devuelve un Response
        -> Mediator.Publish(Response)
             -> RegistrarVentaPresenter.Handle(Response)  llena el ResultViewModel
```

Esto se ve paso a paso, con codigo, en
[04-flujo-de-un-request.md](04-flujo-de-un-request.md).

## Comunicacion cross-module (Contratos + DI)

Cuando el modulo `Ventas` necesita algo de `Inventario`, `Identity` o `Geo`, lo
hace en tres pasos:

### Paso 1: el modulo proveedor expone un contrato

El proveedor publica una interfaz en su proyecto `{Modulo}.Contracts`, que no
depende de nada. Ejemplo, `Inventario.Contracts/IConsultaInventario.cs`:

```csharp
namespace Inventario.Contracts;

public sealed record ProductoInfo(long ProductoId, string Nombre, decimal Precio, int StockDisponible);
public sealed record ResultadoAjusteStock(bool Exitoso, string? Error, int StockRestante);

public interface IConsultaInventario
{
    Task<IReadOnlyList<ProductoInfo>> ListarAsync(CancellationToken ct = default);
    Task<ProductoInfo?> ObtenerProductoAsync(long productoId, CancellationToken ct = default);
    Task<ResultadoAjusteStock> DescontarStockAsync(long productoId, int cantidad, CancellationToken ct = default);
}
```

### Paso 2: el proveedor implementa su contrato y lo registra

La implementacion vive dentro del proveedor (en su Application o Infrastructure) y
NO sale de ahi. Ejemplo, `Inventario.Application/ConsultaInventarioService.cs`:

```csharp
public sealed class ConsultaInventarioService : IConsultaInventario
{
    private readonly IProductoRepository _productos;   // repo del propio modulo
    public ConsultaInventarioService(IProductoRepository productos) => _productos = productos;

    public async Task<ProductoInfo?> ObtenerProductoAsync(long id, CancellationToken ct = default)
    {
        var p = await _productos.FindByIdAsync(id, ct);
        return p is null ? null : new ProductoInfo(p.Id, p.Nombre, p.Precio, p.Stock);
    }
    // ... ListarAsync, DescontarStockAsync
}
```

Y se registra en el `ServiceCollectionEx` del modulo:

```csharp
// Inventario.Application/ServiceCollectionEx.cs
services.AddScoped<IConsultaInventario, ConsultaInventarioService>();
```

Esa linea significa: "cuando alguien pida `IConsultaInventario`, el contenedor le
entrega un `ConsultaInventarioService`".

### Paso 3: el modulo consumidor depende solo del contrato

`Ventas` referencia unicamente `Inventario.Contracts` (en su `.csproj`) y recibe la
interfaz por constructor. Nunca menciona `ConsultaInventarioService`.

```csharp
// Ventas.Application/UseCases/RegistrarVenta/RegistrarVentaHandler.cs
public RegistrarVentaHandler(
    IConsultaInventario inventario,    // contrato de negocio
    IGeoCatalog geo,                   // contrato global
    ICurrentUserAccessor currentUser,  // contrato global
    IVentaRepository ventas)           // repo propio
{ ... }
```

El `.csproj` de `Ventas.Application` lo deja explicito:

```xml
<ProjectReference Include="..\..\Inventario\Inventario.Contracts\Inventario.Contracts.csproj" />
<ProjectReference Include="..\..\Globales\Identity\Identity.Contracts\Identity.Contracts.csproj" />
<ProjectReference Include="..\..\Globales\Geo\Geo.Contracts\Geo.Contracts.csproj" />
```

No hay ninguna referencia a `Inventario.Domain`, `Inventario.Application`,
`Inventario.Infrastructure`, etc.

### Quien conecta las puntas: el Host

El consumidor pide una interfaz; el proveedor la implementa. Quien une ambos es el
Host, porque registra los servicios de los dos modulos. En tiempo de ejecucion, el
contenedor de DI le entrega a `Ventas` la implementacion real de `Inventario`.

```
Ventas pide IConsultaInventario  ----(DI en el Host)---->  recibe ConsultaInventarioService
```

## Por que hacerlo asi (para perfiles Sr)

- **Bajo acoplamiento**: los modulos dependen de interfaces estables, no de
  implementaciones. Cambiar la implementacion de un modulo no rompe a los demas.
- **Testabilidad**: para testear `Ventas` se puede pasar un doble de prueba (mock)
  de `IConsultaInventario`; no hace falta levantar el modulo Inventario completo.
- **Fronteras claras**: el contrato es el unico punto de contacto, asi que el
  "area de superficie" entre modulos es minima y explicita.
- **Posible escision a microservicio**: si un dia `Inventario` debe ser su propio
  servicio, el consumidor solo cambia: en vez de una implementacion local del
  contrato, una que llama por HTTP. `Ventas` no se entera.

## Los globales: el caso mas comun

Los modulos globales (`Identity`, `Geo`) son los que mas se consumen, porque
ofrecen capacidades que casi todos necesitan:

- `Identity.Contracts.ICurrentUserAccessor`: "quien esta haciendo esta peticion".
  Lo usa cualquier modulo que necesite saber el usuario actual.
- `Geo.Contracts.IGeoCatalog`: catalogo de paises para validar o listar.

En ArccNova el patron es identico: todos los modulos dependen de
`Identity.Contracts`, y `Subdivision` consume ademas `Geo.Contracts`,
`Files.Contracts`, `Payments.Contracts`, etc. Por eso este ejemplo incluye dos
globales: para mostrar el caso real mas frecuente.

## Como se verifica el aislamiento (no es solo una recomendacion)

La regla "los modulos solo se comunican via Contracts" no se queda en la teoria:
hay un test automatico que la verifica, igual que ArccNova con `NetArchTest.Rules`.

`Tests/Arquitectura.Tests/AislamientoDeModulosTests.cs`:

```csharp
[Fact]
public void Ventas_solo_depende_de_Contracts_no_de_implementaciones()
{
    var resultado = Types.InAssembly(typeof(RegistrarVentaHandler).Assembly)
        .That().ResideInNamespaceStartingWith("Ventas")
        .ShouldNot().HaveDependencyOnAny(
            "Inventario.Domain", "Inventario.Application", "Inventario.Infrastructure",
            "Identity.Infrastructure",
            "Geo.Domain", "Geo.Application", "Geo.Infrastructure")
        .GetResult();

    Assert.True(resultado.IsSuccessful);
}
```

Si alguien, por error, agrega en `Ventas` una referencia a la implementacion de
otro modulo, este test falla en CI. Asi la arquitectura se mantiene sola con el
tiempo, sin depender de la memoria del equipo.

Para correrlo:

```bash
dotnet test ComunicacionModulos.slnx
```

## Siguiente paso

Continua con [04-flujo-de-un-request.md](04-flujo-de-un-request.md) para ver el
recorrido completo de una peticion real, con el codigo de cada paso.
