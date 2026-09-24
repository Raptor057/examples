# Comunicacion entre modulos (monolito modular estilo ArccNova)

Proyecto de ejemplo, autoexplicado, que muestra como dos partes de una misma
aplicacion (modulos) se comunican entre si SIN conocerse por dentro, usando el
mismo patron que `ArccNova.WebApi`: monolito modular con Clean Architecture,
CQRS, un Mediator propio (libreria `Common`), Presenters y `ResultViewModel`.

Esta pensado para que lo entienda cualquier perfil del equipo: si eres Junior,
empieza por la guia de conceptos; si eres Senior, ve directo a arquitectura y
recetas. Cada documento explica primero la idea en palabras simples y luego entra
al detalle tecnico.

## Que demuestra, en una frase

Un caso de uso del modulo `Ventas` (registrar una venta) necesita datos de otros
tres modulos (Inventario, Identity, Geo) y los usa SOLO a traves de sus contratos
(`*.Contracts`), nunca de su codigo interno. El contenedor de inyeccion de
dependencias conecta cada contrato con su implementacion en tiempo de ejecucion.

## Mapa de la documentacion

Lee en este orden si es tu primera vez:

| # | Documento | Para que sirve | Nivel |
|---|-----------|----------------|-------|
| 1 | [docs/01-conceptos-clave.md](docs/01-conceptos-clave.md) | Glosario explicado con analogias: monolito modular, contrato, inyeccion de dependencias, mediator, presenter, capas | Junior primero |
| 2 | [docs/02-arquitectura.md](docs/02-arquitectura.md) | Estructura de carpetas, grafo de dependencias y las reglas no negociables (con su porque) | Todos |
| 3 | [docs/03-comunicacion-entre-modulos.md](docs/03-comunicacion-entre-modulos.md) | El tema central: los dos tipos de comunicacion (intra-modulo y cross-module) | Todos |
| 4 | [docs/04-flujo-de-un-request.md](docs/04-flujo-de-un-request.md) | Recorrido paso a paso de `POST /api/ventas` con el codigo de cada pieza | Todos |
| 5 | [docs/05-modulos.md](docs/05-modulos.md) | Catalogo de modulos: que hace cada uno, que contrato expone y quien lo consume | Todos |
| 6 | [docs/06-recetas-y-extension.md](docs/06-recetas-y-extension.md) | Recetas paso a paso: agregar un use case, un modulo o un global nuevo | Todos |
| 7 | [docs/07-endpoints-pruebas-y-ejecucion.md](docs/07-endpoints-pruebas-y-ejecucion.md) | Endpoints, ejemplos curl, Swagger, diagnostico, build/test/run y troubleshooting | Todos |

## Arranque rapido

Requisitos: .NET 10 SDK.

Todo se corre desde esta carpeta (`comunicacion-modulos/` dentro del repo `examples`).
`Shared/Common` viene incluido como copia, asi que no hay submodulo que inicializar.

```bash
# 1. Compilar y correr los tests de arquitectura
dotnet build ComunicacionModulos.slnx
dotnet test  ComunicacionModulos.slnx

# 2. Levantar la API (abre el navegador en /swagger automaticamente)
dotnet run --project Host/Host.Api
```

Swagger queda en http://localhost:5200/swagger.

## Estructura de un vistazo

```
Comunicacion entre modulos/
  Shared/
    Common/        copia de Common: Mediator, Result, AddMediator, ResultViewModel
    Shared.Web/    BaseApiController (chasis web compartido)
  Modules/
    Globales/      modulos transversales que todos consumen
      Identity/    ICurrentUserAccessor (usuario actual)
      Geo/         IGeoCatalog (catalogo de paises)
    Inventario/    modulo de negocio (proveedor de productos y stock)
    Ventas/        modulo de negocio (consume Inventario + Identity + Geo)
  Host/Host.Api/   composition root: arma el grafo de DI y expone la API
  Tests/Arquitectura.Tests/   verifica el aislamiento entre modulos
  ComunicacionModulos.slnx
```

## Relacion con ArccNova

Todo lo que aqui se ve simplificado existe igual en `ArccNova.WebApi`: las capas
por modulo, los `{Modulo}.Contracts`, el Mediator de `Common`, los Presenters,
`ResultViewModel`, los modulos globales (`Identity`, `Geo`, ...) y la regla de que
los modulos solo se comunican via contratos. El detalle del mapeo esta en cada
documento. Las diferencias deliberadas (repos en memoria, sin base de datos, sin
JWT real) estan listadas en [docs/02-arquitectura.md](docs/02-arquitectura.md).
