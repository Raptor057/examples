# 01 - Conceptos clave (glosario explicado)

Este documento explica, en palabras simples y con analogias, todos los terminos
que aparecen en el proyecto. Si nunca trabajaste con Clean Architecture, CQRS o
inyeccion de dependencias, empieza aqui. No necesitas conocer ArccNova para
entenderlo.

## Monolito modular

Un **monolito** es una sola aplicacion que se compila y despliega como una unidad
(un solo proceso, un solo servidor). Lo contrario serian los **microservicios**:
muchas aplicaciones pequeñas que se despliegan por separado y se hablan por red.

Un **monolito modular** es el punto intermedio: es UNA sola aplicacion, pero por
dentro esta dividida en **modulos** bien separados, con fronteras claras, como si
cada uno pudiera convertirse algun dia en su propio servicio.

Analogia: un edificio de departamentos. Es un solo edificio (un monolito), pero
cada departamento (modulo) tiene su puerta, su llave y su espacio privado. Los
vecinos no entran a tu cocina; si necesitan algo, tocan a la puerta y piden por la
mirilla (el contrato). Comparten cimientos e instalaciones (el codigo comun).

Por que se usa: se obtiene el orden y la separacion de los microservicios, pero sin
la complejidad de operar muchos servicios en red. Si un modulo crece mucho, se
puede sacar a su propio servicio mas adelante porque ya estaba aislado.

## Modulo

Una unidad de negocio o capacidad con responsabilidad propia. En este proyecto hay
dos tipos:

- **Modulos de negocio**: resuelven un problema del dominio. Aqui: `Inventario`
  (productos y stock) y `Ventas` (registrar ventas).
- **Modulos globales**: capacidades transversales que muchos modulos reutilizan.
  Aqui: `Identity` (saber quien hace la peticion) y `Geo` (catalogo de paises).
  Son los mas consumidos.

## Las capas de un modulo

Cada modulo se divide en capas. Cada capa tiene UNA responsabilidad y solo puede
depender de las capas permitidas. Esto se llama Clean Architecture.

| Capa | Que contiene | Analogia |
|------|--------------|----------|
| `Domain` | Las entidades del negocio (ej. `Producto`, `Venta`) y las interfaces de repositorio. Es el corazon, sin dependencias externas. | Las reglas del juego y las fichas |
| `Application` | Los casos de uso (la logica): "registrar una venta", "listar productos". Orquesta el dominio. | El reglamento de como se juega cada jugada |
| `Infrastructure` | La implementacion tecnica: acceso a datos (repos), servicios externos. | El tablero fisico y las cajas donde se guardan las fichas |
| `Presentation` | Los controllers (la entrada HTTP) y los presenters (como se arma la respuesta). | La mesa donde se muestra el juego al publico |
| `Contracts` | Lo UNICO que el modulo expone a otros modulos: interfaces y objetos planos. | La mirilla de la puerta: lo que los vecinos pueden ver y pedir |

Regla de oro de las dependencias: las capas apuntan hacia adentro. `Presentation`
y `Infrastructure` conocen a `Application`; `Application` conoce a `Domain`;
`Domain` no conoce a nadie. Asi, la logica de negocio no depende de detalles como
la base de datos o el framework web.

## Contrato (Contracts)

Un **contrato** es la "cara publica" de un modulo: una interfaz (y unos objetos de
datos simples) que dice "esto es lo que puedo hacer por ti", sin revelar como lo
hace. Vive en el proyecto `{Modulo}.Contracts`, que no depende de nada.

Analogia: el menu de un restaurante. Pides "una pizza" por el menu (el contrato);
no entras a la cocina a ver como la preparan. Si la cocina cambia el horno, a ti
no te afecta mientras la pizza siga llegando.

Ejemplo real del proyecto, `Inventario.Contracts`:

```csharp
public sealed record ProductoInfo(long ProductoId, string Nombre, decimal Precio, int StockDisponible);

public interface IConsultaInventario
{
    Task<ProductoInfo?> ObtenerProductoAsync(long productoId, CancellationToken ct = default);
    Task<ResultadoAjusteStock> DescontarStockAsync(long productoId, int cantidad, CancellationToken ct = default);
    Task<IReadOnlyList<ProductoInfo>> ListarAsync(CancellationToken ct = default);
}
```

El modulo `Ventas` depende SOLO de esta interfaz. No sabe (ni le importa) si por
dentro Inventario usa una base de datos, un archivo o memoria.

Por que importa (nota para perfiles Sr): el contrato es la frontera de
acoplamiento. Mientras el contrato no cambie, los dos modulos evolucionan por
separado, se testean por separado y, si algun dia hace falta, uno se puede sacar a
otro servicio cambiando solo la implementacion del contrato.

## Inyeccion de dependencias (DI)

En lugar de que una clase **cree** las cosas que necesita, se las **entregan** ya
listas por el constructor. Quien las entrega es el contenedor de DI del framework.

Analogia: no fabricas tu propia electricidad; enchufas el aparato y la corriente
llega. El enchufe (la interfaz) es estandar; de donde viene la luz (la
implementacion) no te importa.

Sin DI:

```csharp
public class RegistrarVentaHandler
{
    private readonly ConsultaInventarioService _inv = new ConsultaInventarioService(...); // acoplado a la clase concreta
}
```

Con DI (lo que usa el proyecto):

```csharp
public RegistrarVentaHandler(IConsultaInventario inventario) // recibe la interfaz, no la crea
{
    _inventario = inventario;
}
```

Quien decide que implementacion concreta se inyecta es el **Host** (ver
[02-arquitectura.md](02-arquitectura.md)), en el registro:

```csharp
services.AddScoped<IConsultaInventario, ConsultaInventarioService>();
// "cuando alguien pida IConsultaInventario, dale un ConsultaInventarioService"
```

## Mediator

Un **mediador** es un intermediario. En vez de que el controller llame
directamente a la clase que hace el trabajo, le entrega un "mensaje" (un Request)
al mediador, y el mediador encuentra a quien sabe atenderlo (el Handler).

Por que se usa: desacopla quien pide (controller) de quien resuelve (handler), y
permite meter pasos automaticos en medio (logging, validacion) sin tocar el
handler. Eso ultimo se llama pipeline.

En el proyecto el mediador es `IMediator` del libreria `Common` (no es la libreria
MediatR). Se usa asi:

```csharp
await Mediator.Send(new RegistrarVentaRequest(productoId, cantidad, paisCliente), ct);
```

## Request, Response, Success y Failure

Cada caso de uso (cada accion) define cuatro piezas:

- **Request**: los datos de entrada. Ej. `RegistrarVentaRequest(ProductoId, Cantidad, PaisCliente)`.
- **Response**: el tipo base de la respuesta (abstracto). Ej. `RegistrarVentaResponse`.
- **Success**: la respuesta cuando todo salio bien, hereda de Response. Ej.
  `RegistrarVentaSuccess(VentaId, Total, ...)`.
- **Failure**: la respuesta cuando algo fallo, hereda de Response. Puede haber
  varios tipos de fallo. Ej. `ProductoNoExisteFailure`, `StockInsuficienteFailure`.

La gracia: el handler devuelve un `Response` que es Success o Failure, y mas
adelante el Presenter decide como mostrarlo (que codigo HTTP, que mensaje). El
handler no sabe de HTTP.

## Handler (Interactor)

La clase que ejecuta el caso de uso. Implementa `IInteractor<Request, Response>`
(que es el `IRequestHandler` de Common con un nombre mas claro). Recibe el Request,
hace el trabajo (consultando repos y contratos de otros modulos) y devuelve un
Response.

## Presenter

La clase que traduce el Response del handler a una respuesta HTTP. Implementa
`IPresenter<Response>`. Decide: si fue Success, llama `OK(datos)`; si fue Failure,
llama `Fail(mensaje)` y fija el codigo HTTP (404, 400, ...).

Por que separar Presenter del Handler: el handler se concentra en la logica de
negocio y es facil de testear sin HTTP; el presenter se concentra en la
presentacion. Es el patron Presenter de ArccNova.

## ResultViewModel y el envelope de respuesta

`ResultViewModel<T>` (del libreria `Common`) es el objeto que termina viajando al
cliente. Siempre tiene la misma forma (el "envelope"):

```json
{ "data": { ... }, "isSuccess": true, "message": null, "utcTimeStamp": "..." }
```

- `data`: el contenido cuando hubo exito.
- `isSuccess`: true o false.
- `message`: el texto del error cuando fallo.
- `utcTimeStamp`: cuando se genero.

Que el formato sea SIEMPRE el mismo le facilita la vida al frontend: siempre lee
los mismos campos. El controller y el presenter comparten la MISMA instancia de
`ResultViewModel` dentro de una peticion (porque se registran como `Scoped`, es
decir una instancia por peticion HTTP).

## Como encaja todo (resumen visual)

```
Cliente HTTP
   |
   v
Controller (Presentation)  --- Mediator.Send(Request) --->  Handler (Application)
   ^                                                            |
   |                                                            | usa repos (Infrastructure)
   |                                                            | y contratos de otros modulos
   |                                                            v
ResultViewModel  <--- Presenter (Presentation) <--- Mediator.Publish(Response)
```

Para ver este recorrido con el codigo real, paso a paso, lee
[04-flujo-de-un-request.md](04-flujo-de-un-request.md).

## Siguiente paso

Ya conoces el vocabulario. Continua con
[02-arquitectura.md](02-arquitectura.md) para ver como esta organizado el proyecto
y que reglas garantizan que todo siga ordenado.
