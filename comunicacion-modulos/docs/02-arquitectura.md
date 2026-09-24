# 02 - Arquitectura

Este documento describe como esta organizado el proyecto: la estructura de
carpetas, que proyecto puede depender de cual, y las reglas que mantienen el
orden. Si vienes de [01-conceptos-clave.md](01-conceptos-clave.md), aqui ves esos
conceptos aplicados.

## Estructura fisica de carpetas

```
Comunicacion entre modulos/
  ComunicacionModulos.slnx              solucion (formato nuevo .slnx, como ArccNova)
  Shared/
    Common/                             copia de la libreria Common (NO se edita)
      Common.Messaging/                 IMediator, IRequest, IInteractor, IPresenter, IResponse
      Common.Contracts/                 Result, ISuccess, IFailure, IValidationFailure, ...
      Common.Infra/                     AddMediator(), Mediator, InteractorPipeline, AddObservability()
      Common.Web/                       ResultViewModel<T>
      Common.MultiTenancy/              (lo usa Common internamente)
    Shared.Web/                         BaseApiController (chasis web propio)
  Modules/
    Globales/                           modulos transversales (los mas consumidos)
      Identity/
        Identity.Contracts/             ICurrentUserAccessor
        Identity.Infrastructure/        CurrentUserAccessor + registro DI
      Geo/
        Geo.Contracts/                  IGeoCatalog + PaisInfo
        Geo.Domain/                     Pais + IPaisRepository
        Geo.Application/                GeoCatalogService + UseCases/ListarPaises
        Geo.Infrastructure/             InMemoryPaisRepository + registro DI
        Geo.Presentation/               GeoController + Presenter + registro DI
    Inventario/                         negocio (proveedor)
      Inventario.Contracts/             IConsultaInventario + POCOs
      Inventario.Domain/                Producto + IProductoRepository
      Inventario.Application/           ConsultaInventarioService + UseCases/ListarProductos
      Inventario.Infrastructure/        InMemoryProductoRepository + registro DI
      Inventario.Presentation/          InventarioController + Presenter + registro DI
    Ventas/                             negocio (consumidor)
      Ventas.Domain/                    Venta + IVentaRepository
      Ventas.Application/               UseCases/RegistrarVenta, UseCases/ListarVentas
      Ventas.Infrastructure/            InMemoryVentaRepository + registro DI
      Ventas.Presentation/              VentasController + Presenters + registro DI
  Host/
    Host.Api/                           composition root: arma DI, expone API y Swagger
  Tests/
    Arquitectura.Tests/                 NetArchTest: verifica el aislamiento entre modulos
```

Nota sobre los nombres: cada modulo no tiene TODAS las capas siempre. Por ejemplo
`Identity` solo tiene `Contracts` + `Infrastructure` (es un accessor simple) y
`Ventas` no tiene `Contracts` (nadie lo consume todavia). Se crea solo lo que el
modulo necesita.

## Reglas de dependencia entre capas (dentro de un modulo)

`A -> B` significa "A referencia a B".

```
Presentation  -> Application -> Domain
Infrastructure -> Application -> Domain
Application   -> Contracts (propio)        (para devolver/implementar sus POCOs)
Contracts     -> (nada)
Domain        -> (nada)
```

Lo que NUNCA pasa:

- `Application` NO referencia `Infrastructure` (la logica no conoce los detalles
  tecnicos; depende de interfaces que viven en `Domain`).
- `Domain` y `Contracts` no dependen de nada externo.

## Reglas de dependencia entre modulos (lo central)

Esta es la regla mas importante del proyecto (regla #3 de ArccNova):

> Un modulo NUNCA referencia el codigo interno de otro modulo. La unica forma de
> usar otro modulo es a traves de su proyecto `{Modulo}.Contracts`.

Ejemplo concreto: `Ventas.Application` referencia tres contratos y nada mas de
esos modulos:

```
Ventas.Application -> Inventario.Contracts    (negocio)
Ventas.Application -> Identity.Contracts       (global)
Ventas.Application -> Geo.Contracts            (global)
```

Lo que esta PROHIBIDO (y un test lo impide, ver
[03-comunicacion-entre-modulos.md](03-comunicacion-entre-modulos.md)):

```
Ventas.* -> Inventario.Domain / Inventario.Application / Inventario.Infrastructure
Ventas.* -> Identity.Infrastructure
Ventas.* -> Geo.Domain / Geo.Application / Geo.Infrastructure
```

## Grafo de dependencias (vista de pajaro)

```
                         +-------------------------+
                         |   Shared/Common (sub)   |  Mediator, Result, ResultViewModel
                         +-------------------------+
                                    ^
            +-----------------------+------------------------+
            |                       |                        |
     Application de           Presentation de           Host.Api
     cada modulo              cada modulo            (composition root)
            |                       |                        |
            v                       v                        |
        Contracts de            Shared.Web                   | referencia
        otros modulos        (BaseApiController)             | TODO y arma el DI
            ^                                                 |
            |                                                 v
   Ventas.Application  ----consume---->  Inventario.Contracts, Identity.Contracts, Geo.Contracts
```

Idea clave: los modulos se "tocan" solo en la capa de Contracts. El Host es el
unico que conoce a todos por completo, porque es quien arma el rompecabezas (el
composition root).

## La libreria Common

`Shared/Common` es una **copia** de
`https://github.com/Raptor-Dev-Services/Common.git` (el mismo que usa ArccNova),
incluida tal cual dentro del ejemplo. En un proyecto real va como submodulo fijado a
un commit; aqui se copio para que el ejemplo compile sin pasos extra dentro del
monorepo `examples`. Es codigo compartido y NO se edita desde este ejemplo.

Que se usa de Common:

| Proyecto | Que aporta | Quien lo usa |
|----------|-----------|--------------|
| `Common.Messaging` | `IMediator`, `IRequest`, `IInteractor`, `IPresenter`, `IResponse` | la capa Application y Presentation de cada modulo |
| `Common.Contracts` | `Result`, `ISuccess`, `IFailure`, `IValidationFailure`, `INotFoundFailure` | Application (para tipar Success/Failure) |
| `Common.Infra` | `AddMediator()` (registra el mediator y el pipeline) y `AddObservability()` (metricas) | el Host |
| `Common.Web` | `ResultViewModel<T>` (el envelope) | Presentation de cada modulo |

Como es una copia, al clonar `examples` ya viene completa: no hay que inicializar
nada. En un proyecto donde Common si es submodulo, el sintoma de olvidarse de
`git submodule update --init --recursive` es la carpeta `Shared/Common` vacia y la
compilacion fallando con "no se encuentra Common.Messaging".

## El Host como composition root

`Host/Host.Api` es el unico proyecto que referencia a todos los modulos. Su
trabajo (en `Program.cs`) es:

1. Registrar el Mediator y escanear los assemblies de Application que tienen
   handlers: `AddMediator(...assemblies...)`.
2. Registrar las capas de cada modulo con sus extensiones
   (`AddXxxApplicationServices()`, `AddXxxInfrastructureServices()`,
   `AddXxxPresentationServices()`).
3. Registrar los controllers de cada modulo como ApplicationParts (porque viven en
   los assemblies de Presentation, no en el Host).
4. Exponer Swagger, los endpoints de diagnostico (`/api/health`, `/api/info`,
   `/metrics`) y abrir el navegador al iniciar.

El detalle de Program.cs esta en
[07-endpoints-pruebas-y-ejecucion.md](07-endpoints-pruebas-y-ejecucion.md).

## Diferencias deliberadas con ArccNova

Este ejemplo simplifica todo lo que no sea el patron de modulos, para que el foco
quede claro:

- `Modules/Globales/` agrupa los globales FISICAMENTE. En ArccNova es una carpeta
  de solucion virtual (en el `.slnx`) y los proyectos viven planos en `Modules/`.
  Aqui se separa fisico para que la distincion global vs negocio se vea de
  inmediato.
- Los repositorios son EN MEMORIA, no EF Core + PostgreSQL.
- `Identity` lee headers HTTP (`X-User-Name`) en lugar de validar un JWT real.
- No hay multi-tenant, ni autorizacion por roles, ni base de datos.
- La libreria Common trae logging, OpenTelemetry y Dapper, pero el ejemplo solo
  cablea el Mediator y las metricas Prometheus.

Todo lo demas (capas, Contracts, Mediator, Presenter, ResultViewModel, registro en
el Host, modulos globales) es igual que en ArccNova.

## Siguiente paso

Continua con [03-comunicacion-entre-modulos.md](03-comunicacion-entre-modulos.md),
que explica en detalle como un modulo habla con otro.
