# 07 - Endpoints, pruebas y ejecucion

Como correr el proyecto, probar cada endpoint y resolver los problemas mas
comunes.

## Requisitos

- .NET 10 SDK (verifica con `dotnet --list-sdks`).

El ejemplo vive en la carpeta `comunicacion-modulos/` del repo `examples`, y los
comandos de abajo se corren desde ahi. El codigo compartido (`Shared/Common`) viene
incluido como copia: no hay submodulo que inicializar.

## Compilar, testear y correr

```bash
# Compilar toda la solucion
dotnet build ComunicacionModulos.slnx

# Correr los tests de arquitectura (verifican el aislamiento entre modulos)
dotnet test ComunicacionModulos.slnx

# Levantar la API
dotnet run --project Host/Host.Api
```

La API queda en http://localhost:5200. Al iniciar en modo Development abre el
navegador automaticamente en `/swagger`.

Para NO abrir el navegador (util en CI, scripts o headless):

```bash
NO_OPEN_BROWSER=1 dotnet run --project Host/Host.Api
```

Para fijar la version que reporta `/api/info`:

```bash
APP_VERSION=2.3.1 dotnet run --project Host/Host.Api
```

## Endpoints

### De negocio y globales

| Metodo | Ruta | Modulo | Descripcion |
|--------|------|--------|-------------|
| GET | `/api/inventario/productos` | Inventario | Lista productos con stock |
| POST | `/api/ventas` | Ventas | Registra una venta (consume Inventario + Geo + Identity) |
| GET | `/api/ventas` | Ventas | Lista las ventas registradas |
| GET | `/api/geo/paises` | Geo (global) | Catalogo de paises |

### De diagnostico (chasis del Host)

| Metodo | Ruta | Descripcion | En Swagger |
|--------|------|-------------|-----------|
| GET | `/api/health` | Estado de salud (`status`, `checks`); 200 si sano, 503 si no | si (tag Diagnostico) |
| GET | `/api/info` | Entorno, version, uptime, maquina, framework | si (tag Diagnostico) |
| GET | `/metrics` | Metricas en formato Prometheus (OpenTelemetry) | no (es para scraping, no JSON) |
| GET | `/` | Redirige a `/swagger` | no (excluido a proposito) |

Sobre `/metrics`: no aparece en Swagger porque devuelve texto en formato Prometheus
para que lo lea un scraper (Prometheus, Grafana), no JSON para humanos. Se ve
abriendo http://localhost:5200/metrics directo. Es como se expone en
produccion.

Sobre `/`: redirige a `/swagger` para que abrir la raiz lleve a la documentacion.
Se excluye de Swagger (con `.ExcludeFromDescription()`) para no ensuciar la lista de
endpoints reales.

## Pruebas con curl

```bash
H=http://localhost:5200

# Global Geo: catalogo de paises
curl -s $H/api/geo/paises

# Inventario: stock inicial
curl -s $H/api/inventario/productos

# Venta valida: consume Inventario (precio/stock) + Geo (valida MX) + Identity (vendedor)
curl -s -X POST $H/api/ventas -H "Content-Type: application/json" \
  -H "X-User-Name: Rogelio" -d '{"productoId":1,"cantidad":2,"paisCliente":"MX"}'

# Pais invalido -> HTTP 400 (lo rechaza Geo)
curl -s -X POST $H/api/ventas -H "Content-Type: application/json" \
  -d '{"productoId":1,"cantidad":1,"paisCliente":"XX"}'

# Sin header de usuario -> vendedor "Usuario Demo" (default de Identity)
curl -s -X POST $H/api/ventas -H "Content-Type: application/json" \
  -d '{"productoId":3,"cantidad":1,"paisCliente":"US"}'

# Producto inexistente -> HTTP 404 (Inventario)
curl -s -X POST $H/api/ventas -H "Content-Type: application/json" \
  -d '{"productoId":99,"cantidad":1,"paisCliente":"US"}'

# Ventas registradas
curl -s $H/api/ventas

# Diagnostico
curl -s $H/api/health
curl -s $H/api/info
curl -s $H/metrics | head -20
```

## Usar Swagger

Abre http://localhost:5200/swagger. Veras los endpoints agrupados por tags:
`Diagnostico`, `Geo`, `Inventario`, `Ventas`. Cada uno se puede ejecutar desde la
UI con el boton "Try it out".

Para mandar el header de usuario en `POST /api/ventas` desde Swagger, agregalo en la
seccion de parametros/headers de esa operacion, o usa curl como arriba.

## Que mira el test de arquitectura

`Tests/Arquitectura.Tests` valida dos cosas con NetArchTest:

1. Ningun tipo de `Ventas` depende de la implementacion (Domain/Application/
   Infrastructure) de Inventario, Identity ni Geo: solo de sus `*.Contracts`.
2. El constructor de `RegistrarVentaHandler` recibe los tres contratos
   (`IConsultaInventario`, `IGeoCatalog`, `ICurrentUserAccessor`).

Si alguien rompe el aislamiento, `dotnet test` falla. Asi la regla se mantiene en el
tiempo.

## Como esta armado Program.cs (composition root)

Resumen de lo que hace `Host/Host.Api/Program.cs`, en orden:

```csharp
// 1. Observabilidad: metricas Prometheus (Common)
builder.Services.AddObservability(builder.Configuration, "ComunicacionModulos.Api");

// 2. Mediator: escanea los assemblies de Application con handlers
builder.Services.AddMediator(
    typeof(Inventario.Application.ServiceCollectionEx).Assembly,
    typeof(Ventas.Application.ServiceCollectionEx).Assembly,
    typeof(Geo.Application.ServiceCollectionEx).Assembly);

// 3. Globales
builder.Services.AddIdentityInfrastructureServices();
builder.Services.AddGeoApplicationServices();
builder.Services.AddGeoInfrastructureServices();
builder.Services.AddGeoPresentationServices();

// 4. Negocio
builder.Services.AddInventarioApplicationServices();
builder.Services.AddInventarioInfrastructureServices();
builder.Services.AddInventarioPresentationServices();
builder.Services.AddVentasApplicationServices();
builder.Services.AddVentasInfrastructureServices();
builder.Services.AddVentasPresentationServices();

// 5. Health + controllers (descubre los controllers de cada Presentation)
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy("API arriba"));
builder.Services.AddControllers()
    .AddApplicationPart(typeof(Geo.Presentation.ServiceCollectionEx).Assembly)
    .AddApplicationPart(typeof(Inventario.Presentation.ServiceCollectionEx).Assembly)
    .AddApplicationPart(typeof(Ventas.Presentation.ServiceCollectionEx).Assembly);

// 6. Swagger, endpoints de diagnostico, /metrics, redirect raiz, abrir navegador
```

## Troubleshooting

### "Address already in use" en el puerto 5200

Quedo una instancia previa de la API ocupando el puerto. Liberalo:

```bash
lsof -ti tcp:5200 | xargs kill -9
```

Esto pasa, por ejemplo, si corriste la API por linea de comandos y luego intentas
correrla otra vez desde el IDE.

### Errores "no se encuentra Common.Messaging / Common.Web"

La copia de `Shared/Common` esta incompleta o se borro. Restaurala desde git:

```bash
git checkout -- Shared/Common
```

### "No handler registered for request X"

Falto agregar el assembly de Application del modulo en `AddMediator(...)`, o el
handler no esta en la capa Application. Ver el checklist de la
[receta de extension](06-recetas-y-extension.md).

### El endpoint existe pero da 404

Falto `.AddApplicationPart(...)` para el assembly de Presentation de ese modulo en
`Program.cs`.

### Swagger no muestra un endpoint

- Si es un `MapHealthChecks` o el endpoint de Prometheus: es esperado, esos no se
  documentan.
- Si es un endpoint normal que no aparece: revisa que `AddEndpointsApiExplorer()`
  este presente y, para controllers, que el `AddApplicationPart` del modulo este
  registrado.

### No quiero que abra el navegador

Corre con `NO_OPEN_BROWSER=1` (ver arriba).

## Cierre

Con esto puedes correr, probar y extender el proyecto. Para entender el porque de
cada decision, vuelve a [02-arquitectura.md](02-arquitectura.md) y
[03-comunicacion-entre-modulos.md](03-comunicacion-entre-modulos.md).
