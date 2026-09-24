# 04 - Flujo de un request paso a paso

Aqui recorremos una peticion real de principio a fin:
`POST /api/ventas`, el caso de uso que registra una venta. Es el ejemplo mas
completo porque consume un modulo de negocio (Inventario) y dos globales (Identity,
Geo). Cada paso muestra el archivo y el codigo que interviene.

## La peticion

```bash
curl -X POST http://localhost:5200/api/ventas \
  -H "Content-Type: application/json" \
  -H "X-User-Name: Rogelio" \
  -d '{"productoId":1,"cantidad":2,"paisCliente":"MX"}'
```

- El body lleva `productoId`, `cantidad` y `paisCliente`.
- El header `X-User-Name` simula al usuario autenticado (lo lee el global Identity).

## Diagrama de secuencia

```
Cliente
  |  POST /api/ventas
  v
VentasController (Ventas.Presentation)
  |  Mediator.Send(RegistrarVentaRequest)
  v
InteractorPipeline (Common.Infra)      [registra el request en el log]
  |  next()
  v
RegistrarVentaHandler (Ventas.Application)
  |   1. IGeoCatalog.ExistePaisAsync("MX")          -> global Geo
  |   2. IConsultaInventario.ObtenerProductoAsync(1) -> negocio Inventario
  |   3. IConsultaInventario.DescontarStockAsync(1,2) -> negocio Inventario
  |   4. ICurrentUserAccessor.Nombre                 -> global Identity
  |   5. IVentaRepository.AddAsync(venta)            -> repo propio
  |  devuelve RegistrarVentaSuccess(...)
  v
InteractorPipeline
  |  Mediator.Publish(RegistrarVentaSuccess)
  v
RegistrarVentaPresenter (Ventas.Presentation)
  |  ResultViewModel.OK({ ventaId, total, stockRestante, vendedor, paisCliente })
  v
VentasController
  |  return Ok(_vm)   (porque _vm.IsSuccess == true)
  v
Cliente recibe el envelope JSON
```

## Paso 1: el controller recibe y delega

Archivo: `Modules/Ventas/Ventas.Presentation/VentasController.cs`

```csharp
[Route("api/ventas")]
public sealed class VentasController : BaseApiController
{
    private readonly ResultViewModel<VentasController> _vm;
    private readonly VentasResponseState _state;

    public VentasController(IMediator mediator, ResultViewModel<VentasController> vm, VentasResponseState state)
        : base(mediator) { _vm = vm; _state = state; }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaBody body, CancellationToken ct = default)
    {
        _ = await Mediator.Send(new RegistrarVentaRequest(body.ProductoId, body.Cantidad, body.PaisCliente), ct);
        return _vm.IsSuccess ? Ok(_vm) : StatusCode(_state.HttpStatusCode, _vm);
    }
}
```

Que hace:

- Hereda de `BaseApiController`, que le da el `Mediator`.
- Recibe el body y crea un `RegistrarVentaRequest`.
- Lo envia por el Mediator. No le interesa quien lo resuelve.
- Al terminar, lee `_vm` (que el presenter ya lleno) y decide el codigo HTTP:
  `Ok` si fue exito, o `StatusCode(_state.HttpStatusCode, _vm)` si fallo.

Detalle importante: `_vm` (el `ResultViewModel`) es la MISMA instancia que usa el
presenter, porque ambos se registran como `Scoped` (una por peticion). Por eso el
controller "ve" lo que el presenter escribio. `_state` (un objeto pequeño) sirve
para que el presenter le diga al controller que codigo HTTP usar.

## Paso 2: el pipeline registra y pasa el control

Archivo (de Common): `Common.Infra/Messaging/InteractorPipeline.cs`

El Mediator no llama directo al handler: primero pasa por el pipeline, que registra
el request en el log, ejecuta el handler (`next()`), registra la respuesta y luego
publica la respuesta a los presenters. Esto pasa automaticamente; no hay que
escribir nada en cada caso de uso.

## Paso 3: el handler ejecuta la logica (y consume otros modulos)

Archivo: `Modules/Ventas/Ventas.Application/UseCases/RegistrarVenta/RegistrarVentaHandler.cs`

```csharp
public sealed class RegistrarVentaHandler : IInteractor<RegistrarVentaRequest, RegistrarVentaResponse>
{
    private readonly IConsultaInventario _inventario;     // negocio
    private readonly IGeoCatalog _geo;                    // global
    private readonly ICurrentUserAccessor _currentUser;   // global
    private readonly IVentaRepository _ventas;            // propio

    public async Task<RegistrarVentaResponse> Handle(RegistrarVentaRequest request, CancellationToken ct)
    {
        // 1. Validar el pais contra el global Geo.
        if (!await _geo.ExistePaisAsync(request.PaisCliente, ct))
            return new PaisInvalidoFailure(request.PaisCliente);

        // 2. Pedir el producto al modulo de negocio Inventario.
        var producto = await _inventario.ObtenerProductoAsync(request.ProductoId, ct);
        if (producto is null)
            return new ProductoNoExisteFailure(request.ProductoId);

        // 3. Descontar stock (Inventario).
        var ajuste = await _inventario.DescontarStockAsync(request.ProductoId, request.Cantidad, ct);
        if (!ajuste.Exitoso)
            return new StockInsuficienteFailure(ajuste.Error ?? "No se pudo descontar el stock.");

        // 4. Saber quien vende, via el global Identity.
        var vendedor = _currentUser.Nombre;

        // 5. Registrar la venta con lo reunido de los tres modulos.
        var total = producto.Precio * request.Cantidad;
        var venta = new Venta(Guid.NewGuid(), request.ProductoId, request.Cantidad, total,
                              vendedor, request.PaisCliente.ToUpperInvariant(), DateTime.UtcNow);
        await _ventas.AddAsync(venta, ct);

        return new RegistrarVentaSuccess(venta.Id, total, ajuste.StockRestante, vendedor, venta.PaisCliente);
    }
}
```

Observa que TODO lo que viene de otros modulos entra por una interfaz de contrato
(`IConsultaInventario`, `IGeoCatalog`, `ICurrentUserAccessor`). El handler no
conoce las clases concretas que las implementan.

El handler devuelve un `RegistrarVentaResponse`, que es:

- `RegistrarVentaSuccess` si todo salio bien, o
- uno de los fallos: `PaisInvalidoFailure`, `ProductoNoExisteFailure`,
  `StockInsuficienteFailure`.

El handler NO sabe de codigos HTTP. Solo decide exito o tipo de fallo.

## Paso 4: el presenter arma la respuesta

Archivo: `Modules/Ventas/Ventas.Presentation/Presenters/RegistrarVentaPresenter.cs`

```csharp
public sealed class RegistrarVentaPresenter : IPresenter<RegistrarVentaResponse>
{
    private readonly ResultViewModel<VentasController> _vm;
    private readonly VentasResponseState _state;

    public Task Handle(RegistrarVentaResponse notification, CancellationToken ct)
    {
        switch (notification)
        {
            case RegistrarVentaSuccess s:
                _vm.OK(new { s.VentaId, s.Total, s.StockRestante, s.Vendedor, s.PaisCliente });
                break;

            case ProductoNoExisteFailure f:
                _state.HttpStatusCode = 404;     // no encontrado
                _vm.Fail(f.Message);
                break;

            case IFailure f:                     // StockInsuficiente / PaisInvalido
                _state.HttpStatusCode = 400;     // peticion invalida
                _vm.Fail(f.Message);
                break;
        }
        return Task.CompletedTask;
    }
}
```

Aqui se decide la presentacion: que datos van en `data`, y para los fallos que
codigo HTTP corresponde (404 si el producto no existe, 400 si el pais es invalido o
no hay stock). El presenter escribe en `_vm`, que el controller leera.

## Las respuestas posibles (todas con el mismo envelope)

Exito (HTTP 200):

```json
{ "data": { "ventaId": "...", "total": 700000, "stockRestante": 3,
            "vendedor": "Rogelio", "paisCliente": "MX" },
  "isSuccess": true, "message": null, "utcTimeStamp": "..." }
```

Pais invalido (HTTP 400):

```json
{ "data": null, "isSuccess": false,
  "message": "El pais 'XX' no existe en el catalogo (Geo).", "utcTimeStamp": "..." }
```

Producto inexistente (HTTP 404):

```json
{ "data": null, "isSuccess": false,
  "message": "El producto 99 no existe.", "utcTimeStamp": "..." }
```

## Por que tantas piezas (para perfiles Sr)

Separar Request / Handler / Response / Presenter / Controller parece mucho para un
caso simple, pero da beneficios concretos:

- El handler es logica pura y se testea sin HTTP ni framework.
- El presenter concentra el mapeo a HTTP; cambiar codigos o formato no toca la
  logica.
- El pipeline agrega logging (y podria agregar validacion, metricas, transacciones)
  a TODOS los casos de uso sin tocar cada handler.
- El envelope uniforme simplifica el frontend.

En casos triviales se nota la ceremonia; en un sistema grande como ArccNova, esta
estructura es la que mantiene el codigo predecible entre muchos modulos y muchas
personas.

## Siguiente paso

Continua con [05-modulos.md](05-modulos.md) para ver el catalogo de modulos y que
ofrece cada uno.
