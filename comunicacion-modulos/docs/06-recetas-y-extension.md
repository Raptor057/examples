# 06 - Recetas y extension

Pasos concretos para tareas comunes. Cada receta lista los archivos a crear y los
registros que no hay que olvidar. Sigue el mismo orden que usarias en un proyecto real.

## Receta A: agregar un use case a un modulo existente

Ejemplo: agregar "obtener un producto por id" a `Inventario`.

1. Crear la carpeta del use case y sus archivos en la capa Application:

```
Inventario.Application/UseCases/ObtenerProducto/
  ObtenerProductoRequest.cs            record ObtenerProductoRequest(long Id) : IRequest<ObtenerProductoResponse>
  Responses/ObtenerProductoResponse.cs abstract record : IResponse
  Responses/ObtenerProductoSuccess.cs  record (ProductoInfo Producto) : ..., ISuccess
  Responses/ObtenerProductoFailures.cs record NoEncontradoFailure : ..., INotFoundFailure
  ObtenerProductoHandler.cs            IInteractor<Request, Response>
```

2. En la capa Presentation, agregar el presenter:

```
Inventario.Presentation/Presenters/ObtenerProductoPresenter.cs   IPresenter<ObtenerProductoResponse>
```

3. Registrar el presenter en `Inventario.Presentation/ServiceCollectionEx.cs`:

```csharp
services.AddScoped<INotificationHandler<ObtenerProductoResponse>, ObtenerProductoPresenter>();
```

4. Agregar la accion al controller (`InventarioController.cs`):

```csharp
[HttpGet("productos/{id:long}")]
public async Task<IActionResult> Obtener(long id, CancellationToken ct = default)
{
    _ = await Mediator.Send(new ObtenerProductoRequest(id), ct);
    return _vm.IsSuccess ? Ok(_vm) : StatusCode(404, _vm);
}
```

No hay que tocar el Host: el handler lo descubre `AddMediator` automaticamente
(escanea el assembly de Application). Solo el presenter se registra a mano.

## Receta B: hacer que un modulo consuma a otro (cross-module)

Ejemplo: que `Ventas` empiece a usar un global nuevo.

1. En el `.csproj` del consumidor, referenciar SOLO el `.Contracts` del proveedor:

```xml
<ProjectReference Include="..\..\Globales\NuevoGlobal\NuevoGlobal.Contracts\NuevoGlobal.Contracts.csproj" />
```

2. Inyectar la interfaz del contrato en el handler que la necesita:

```csharp
public RegistrarVentaHandler(IConsultaInventario inventario, INuevoContrato nuevo, ...) { ... }
```

3. Asegurarte de que el Host registre la implementacion del proveedor (ver Receta C
   o D). El DI hace el resto.

Lo que NO debes hacer: referenciar `NuevoGlobal.Application` o
`NuevoGlobal.Infrastructure` desde Ventas. Si lo haces, el test de arquitectura
falla.

## Receta C: agregar un modulo global tipo accessor (como Identity)

Para una capacidad sin datos ni endpoints (solo lee el contexto y responde).

1. Crear dos proyectos:

```
Modules/Globales/MiGlobal/
  MiGlobal.Contracts/        IMiCapacidad (interfaz) + POCOs   (sin dependencias)
  MiGlobal.Infrastructure/   MiCapacidad : IMiCapacidad + ServiceCollectionEx
```

2. `MiGlobal.Infrastructure.csproj` referencia `MiGlobal.Contracts` y, si necesita
   el contexto HTTP, `FrameworkReference Microsoft.AspNetCore.App`.

3. Registrar en `ServiceCollectionEx`:

```csharp
public static IServiceCollection AddMiGlobalInfrastructureServices(this IServiceCollection s)
{
    s.AddScoped<IMiCapacidad, MiCapacidad>();
    return s;
}
```

4. En el Host: referenciar `MiGlobal.Infrastructure` y llamar
   `builder.Services.AddMiGlobalInfrastructureServices();`.

## Receta D: agregar un modulo (global o de negocio) con stack completo

Para un modulo con datos y endpoints (como Geo o Inventario). Crear estos
proyectos:

```
Mi.Contracts        interfaz + POCOs (si alguien lo va a consumir)
Mi.Domain           entidades + interfaces de repositorio
Mi.Application       implementacion del contrato + use cases (Mediator) + ServiceCollectionEx
Mi.Infrastructure   repositorios + ServiceCollectionEx
Mi.Presentation     controller(s) + presenter(s) + ServiceCollectionEx
```

Referencias entre capas (las flechas son ProjectReference):

```
Mi.Application    -> Mi.Domain, Mi.Contracts, Common.Messaging, Common.Contracts
Mi.Infrastructure -> Mi.Domain, Mi.Application
Mi.Presentation   -> Mi.Application, Shared.Web, Common.Web   (+ FrameworkReference AspNetCore.App)
```

Registro de las extensiones en el Host (`Program.cs`):

```csharp
builder.Services.AddMediator(
    ...,
    typeof(Mi.Application.ServiceCollectionEx).Assembly);   // para que descubra los handlers

builder.Services.AddMiApplicationServices();
builder.Services.AddMiInfrastructureServices();
builder.Services.AddMiPresentationServices();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Mi.Presentation.ServiceCollectionEx).Assembly);   // para descubrir el controller
```

Y agregar todos los `.csproj` a la solucion:

```bash
dotnet sln ComunicacionModulos.slnx add Modules/.../Mi.Contracts/Mi.Contracts.csproj
# ... repetir por cada proyecto del modulo
```

## Checklist de registro en el Host (lo que mas se olvida)

Cuando agregas un modulo, revisa que en `Host/Host.Api/Program.cs` esten estas
cuatro cosas:

1. El assembly de Application en `AddMediator(...)` (si el modulo tiene handlers).
2. Las tres extensiones `AddXxxApplicationServices()`,
   `AddXxxInfrastructureServices()`, `AddXxxPresentationServices()` (las que
   apliquen).
3. `.AddApplicationPart(...)` por cada assembly de Presentation con controllers.
4. Las `ProjectReference` correspondientes en `Host.Api.csproj`.

Sintomas de un registro olvidado:

- "No handler registered for request X": falto el assembly en `AddMediator` o el
  modulo no tiene su handler en Application.
- El endpoint da 404 aunque el codigo existe: falto `AddApplicationPart` para ese
  modulo de Presentation.
- La respuesta llega vacia o sin formato: falto registrar el presenter
  (`AddScoped<INotificationHandler<...Response>, ...Presenter>()`).

## Convencion de nombres

| Pieza | Patron | Ejemplo |
|-------|--------|---------|
| Request | `{Accion}{Recurso}Request : IRequest<{Accion}{Recurso}Response>` | `RegistrarVentaRequest` |
| Response base | `abstract record {Accion}{Recurso}Response : IResponse` | `RegistrarVentaResponse` |
| Success | `record {Accion}{Recurso}Success : Response, ISuccess` | `RegistrarVentaSuccess` |
| Failure | `record {Nombre}Failure : Response, IValidationFailure` | `StockInsuficienteFailure` |
| Handler | `{Accion}{Recurso}Handler : IInteractor<Request, Response>` | `RegistrarVentaHandler` |
| Presenter | `{Accion}{Recurso}Presenter : IPresenter<Response>` | `RegistrarVentaPresenter` |
| Body HTTP | `record {Accion}{Recurso}Body` (junto al controller) | `RegistrarVentaBody` |

## Siguiente paso

Continua con [07-endpoints-pruebas-y-ejecucion.md](07-endpoints-pruebas-y-ejecucion.md)
para correr, probar y diagnosticar el proyecto.
