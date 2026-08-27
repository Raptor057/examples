# Reporte jerarquico con carga por niveles (arbol perezoso)

Ejemplo completo y ejecutable del patron `lazy-hierarchy-report`: base de datos, backend y
frontend. Un arbol de seis niveles sobre **1.49 millones de lineas de pedido** repartidas en dos
empresas, donde **nada se carga hasta que alguien lo abre**.

```
Anio -> Mes -> Categoria -> Producto -> Pedido -> Lineas del pedido (hoja)
```

| | |
|---|---|
| Motor | PostgreSQL 16 (compatible con AWS Aurora), en `docker-compose.yml` |
| Backend | .NET 10, monolito modular, un modulo `Orders` con sus seis proyectos |
| Acceso a datos | **EF Core escribe, Dapper lee.** El proyecto es multi-tenant |
| Frontend | React 19 + Vite 7 + Tailwind 4, i18n `es` / `en` |
| Volumen sembrado | 2 tenants, 428,571 pedidos, 1,492,580 lineas, 587 MB de base |

---

## Que demuestra

**El arbol no se carga: se descubre.** Cada expansion es una consulta y cada consulta devuelve
un solo nivel. Traer el arbol entero para que alguien abra tres ramas es pagar el costo completo
por el 1% del valor; con 1.49 millones de lineas ni siquiera es una opcion lenta, es una opcion
que no cabe.

Las piezas que hacen que funcione, y donde vive cada una:

| Idea | Donde mirarla |
|---|---|
| Un solo endpoint sirve los seis niveles | `Orders.Presentation/Controllers/OrdersTreeController.cs` |
| Lista blanca de niveles y validacion progresiva de coordenadas | `Orders.Application/.../GetOrdersTreeChildrenHandler.cs` |
| `nextLevel` en cada nodo: el servidor decide la jerarquia | `Orders.Domain/Trees/OrdersTreeLevels.cs` |
| Los conteos los calcula el motor, no se cuentan hijos en memoria | `Orders.Infrastructure/.../OrdersTreeSql.cs` |
| CTE que pagina primero, `LEFT JOIN LATERAL` que enriquece despues | `OrdersTreeSql.OrdersPage` |
| Predicado por tipo de busqueda con default `1 = 0` | `OrdersTreeSql.SearchPredicate` |
| La hoja devuelve su subarbol completo en una llamada | `OrdersTreeReadRepository.LeafAsync` |
| Store plano, cache y estado por nodo | `frontend/src/features/ordersTree/hooks/useLazyOrdersTree.js` |
| Exportacion por paginas, en bloques de texto | `frontend/src/features/ordersTree/hooks/useOrdersTreeExport.js` |
| El precio de meter Dapper en un proyecto con tenancy | `Shared/LazyHierarchy.Shared/Tenancy/TenantScope.cs` y la prueba que lo vigila |

---

## La cascada de llamadas

Este es el argumento entero del patron. Numeros **medidos en vivo** contra la base sembrada, en
un portatil: mediana de cinco llamadas despues de una de calentamiento, llamando los endpoints
tal cual estan en `LazyHierarchy.Host.http`. El peso es el cuerpo de la respuesta HTTP completo,
envelope incluido.

| Paso | `level` | Nodos | Peso de la respuesta | Tiempo |
|---|---|---|---|---|
| 1. Abrir la pantalla | `tree-year` | 4 | **859 B** | 120 ms |
| 2. Abrir un anio | `tree-month` | 12 | **2.4 kB** | 40 ms |
| 3. Abrir un mes | `tree-category` | 10 | **2.4 kB** | 41 ms |
| 4. Abrir una categoria | `tree-product` | 30 | **7.6 kB** | 25 ms |
| 5. Abrir un producto | `tree-order` | 50 de 90 | **20.6 kB** | 12 ms |
| 6. Abrir un pedido (hoja) | `tree-order-detail` | 7 | **1.6 kB** | 6 ms |
| | | **Total de navegar hasta el detalle** | **35.5 kB** | **244 ms** |

Y lo que costaria traer esa misma rama de golpe, sin arbol perezoso:

| Alternativa | Renglones | Peso | Tiempo |
|---|---|---|---|
| La matriz completa de **un mes** (lo que baja el boton de exportar en el nodo de mes) | 20,423 | **2.8 MB** por pagina de 20,000 | 322 ms |
| La matriz completa del **tenant** | 970,462 | del orden de **140 MB** (extrapolado, no medido) | minutos |

Navegar seis niveles hasta el detalle de un pedido cuesta **35 kB**. Traer una sola rama de mes
cuesta **2.8 MB**, y el tenant completo esta dos ordenes de magnitud mas arriba. El usuario mira
lo mismo en los dos casos.

### El detalle honesto: la raiz es el nivel mas caro

El nivel 1 tarda **120 ms**, mas que los cinco siguientes juntos, y eso no es un descuido del
ejemplo: es lo que pasa de verdad. Agregar por anio obliga a recorrer **todos** los pedidos del
tenant (278,571 filas), y como el tenant es el 65% de la tabla, el planificador elige un barrido
secuencial y ningun indice lo cambia. Se comprobo con `EXPLAIN (ANALYZE, BUFFERS)`, incluso
agregando un indice cubriente con `INCLUDE (total_amount)`: sigue eligiendo el barrido, porque
para esa selectividad es la opcion correcta.

La leccion es esa: **el arbol perezoso abarata las ramas, no la raiz.** Si la raiz agrega el
universo, el arreglo no es un indice sino pre-agregar (una tabla de totales por tenant y anio,
mantenida por trabajo en segundo plano, o una vista materializada). Este ejemplo no la incluye
para no tapar el punto que si demuestra, pero decirlo importa mas que esconderlo.

---

## Como levantarlo

Requisitos: Docker, .NET 10 SDK y Node 20 o superior.

```bash
# 1. Motor. Nada que instalar a mano.
cd lazy-hierarchy-orders
docker compose up -d

# 2. Esquema (migraciones EF Core). Tambien corre solo al arrancar el Host.
cd backend/LazyHierarchy.Host
dotnet run -- migrate

# 3. Datos. Del orden de 1.5 millones de lineas: tarda unos 5 minutos e imprime avance.
dotnet run -- seed

# 4. API en http://localhost:5080
dotnet run

# 5. Frontend en http://localhost:5173 (proxy /api -> :5080)
cd ../../frontend
npm install
npm run dev
```

Volumenes de siembra distintos, para probar rapido:

```bash
Seed__TargetOrderLines=50000 dotnet run -- seed   # sembrado chico, unos 15 segundos
Seed__Force=true dotnet run -- seed               # repetir sobre una base ya sembrada
```

Verificacion:

```bash
cd backend
dotnet build LazyHierarchy.slnx   # 0 errores, 0 warnings
dotnet test  LazyHierarchy.slnx   # 137 pruebas
cd ../frontend && npm run build
```

En la pantalla hay un selector de **empresa**. Cambiarlo pide un token nuevo y el arbol cambia
entero: `acme` ve unos 69,500 pedidos por anio y `globex` unos 37,500. Ninguna ve un renglon de
la otra, y el conteo de cada nodo tampoco la delata.

---

## Lo mas delicado: EF Core y Dapper conviviendo con tenancy

La regla de oro 2 del catalogo es tajante: **la tenencia decide el acceso a datos.** Con
aislamiento, manda EF Core, porque el global query filter lo aplica solo y no hay filtro que
olvidar. Y lista como antipatron explicito *"usar Dapper en un proyecto con tenancy y filtrar el
tenant a mano en cada query"*.

Este ejemplo hace las dos cosas a la vez, y por eso vale la pena explicar como no cae en el
antipatron.

### Por que hay Dapper aqui

El nivel pesado necesita esto:

```sql
WITH page AS (
    SELECT ..., COUNT(*) OVER() AS total_count
    FROM   customer_order o
    WHERE  o.tenant_id = @TenantId AND ... AND EXISTS (SELECT 1 FROM order_line ol WHERE ...)
    ORDER BY o.placed_at_utc DESC, o.id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
)
SELECT pg.*, whole.line_count, focus.focus_amount
FROM page pg
LEFT JOIN LATERAL (...) whole ON TRUE
LEFT JOIN LATERAL (...) focus ON TRUE
```

Paginar **primero** con la consulta mas barata posible y colgar lo caro **despues**, para 50
filas en vez de para un millon. EF no lo expresa sin caer en SQL crudo igualmente, y la consulta
que genera por su cuenta enriquece sobre el universo completo.

### El precio, y como se paga

Bajar a SQL crudo cuesta el aislamiento automatico. La salida no es escribir
`WHERE tenant_id = @TenantId` en cada consulta -eso es el antipatron- sino tres cosas juntas:

**1. Un solo fragmento, en un solo lugar.** `TenantScope.On("o")` produce el predicado y ninguna
consulta escribe ese texto a mano. `TenantScope.Bind()` pone el valor, y lo toma del contexto de
la peticion.

```csharp
public static string On(string alias) => $"{alias}.{Column} = @{ParameterName}";

public DynamicParameters Bind(object? parameters = null)
{
    var bound = ...;
    bound.Add(ParameterName, CurrentTenantId);   // del token, nunca del cliente
    return bound;
}
```

**2. El tenant nunca llega del cliente.** Sale del claim `tenant_id` del JWT, lo publica
`TenantClaimsMiddleware` despues de `UseAuthentication`, y lo leen por igual el query filter de
EF y el `TenantScope` de Dapper. Sin contexto vale el contexto sistema (`0`), que no tiene filas
de negocio: fail-closed. Ningun `Request` del modulo tiene un campo de tenant, y hay una prueba
que falla si alguien se lo agrega.

**3. Una prueba que recorre las consultas por nivel.** Es la red sin la cual el patron es una
fuga esperando a ocurrir. Para **cada nivel** y **cada tipo de busqueda**, la prueba extrae los
pares `(tabla, alias)` de cada `FROM` y cada `JOIN`, y falla si una tabla de negocio aparece sin
su fragmento canonico. Ademas cuenta: si el numero de menciones de `tenant_id` no coincide con
el numero de fragmentos canonicos, alguien lo escribio a mano.

```
Orders.Tests/TenantIsolationSqlTests.cs
  Toda_tabla_de_negocio_lleva_el_fragmento_de_tenant   (36 casos)
  Toda_mencion_de_tenant_id_esta_en_forma_canonica     (36 casos)
  Toda_consulta_usa_el_parametro_de_tenant             (36 casos)
  La_deteccion_encuentra_una_consulta_sin_aislar       (prueba de la prueba)
```

La ultima no sobra: un guardia que no puede fallar no protege de nada, asi que se comprueba que
el reconocedor detecta una consulta deliberadamente sin aislar.

### Donde queda cada uno

| | EF Core | Dapper |
|---|---|---|
| Entidades, `DbContext`, migraciones | si | no |
| Escrituras (alta de pedido, seeder) | si | no |
| Aislamiento | global query filter automatico | `TenantScope` centralizado + prueba |
| Consultas por nivel del arbol | no | si |
| Consulta de exportacion | no | si |

**Esa es la leccion del ejemplo**: cuando bajar a SQL crudo, y que hay que montar para que
bajar no signifique perder el aislamiento.

---

## El chasis de entidad: por que no hay ni un `HasQueryFilter`

Las entidades declaran su ambito con una interfaz y el `AppDbContext` aplica el filtro por
convencion. No existe una linea por entidad que alguien pueda olvidar al agregar una tabla.

```csharp
public sealed class OrderLine : BaseEntity, ITenantOwned, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }
    // ... solo columnas de negocio
}
```

`Tenant` es la excepcion declarada: **no** implementa `ITenantOwned`, porque es la tabla de
tenants. Esta escrito en su configuracion para que nadie lo lea como un filtro olvidado.

El `AuditSoftDeleteInterceptor` hace lo demas: fechas UTC al crear y modificar, borrado logico en
vez de fisico, y **sello del tenant al insertar** desde el contexto autenticado. Por eso
`CreateOrderHandler` no menciona el tenant en ninguna linea y la fila queda con el correcto.

---

## Los indices, y por que son parte del patron

Un arbol perezoso sin los indices que sus consultas necesitan es igual de lento que traerlo todo,
y encima parece que el patron no sirve. La migracion lleva escrito a que consulta sirve cada uno
(`Orders.Infrastructure/Persistence/Migrations/*_InitialSchema.cs`).

| Indice | A que consulta sirve |
|---|---|
| `ix_customer_order_tenant_id_placed_at_utc_id` (parcial `WHERE is_active`) | Niveles 1 y 2 (agregacion por rango de fecha) y el `ORDER BY ... DESC, id DESC` del CTE del nivel 5 |
| `ix_customer_order_tenant_id_status_placed_at_utc` | Busqueda por estado sin perder el orden por fecha |
| `ix_order_line_tenant_id_order_id` | La hoja y los dos `LEFT JOIN LATERAL` que enriquecen la pagina |
| `ix_order_line_tenant_id_product_id_order_id` | El `EXISTS` del CTE y la agregacion de los niveles 3 y 4 |
| `ix_product_tenant_id_category_id_name` | Nivel 4, ya ordenado por nombre |
| `ux_*_tenant_id_*` | Unicidad **por tenant**: dos empresas pueden repetir codigo, SKU y numero de pedido |

Dos detalles que cambian el plan de ejecucion:

- **El periodo entra como rango**, no como funcion sobre la columna. `date_part('year', ...) = @Year`
  en el `WHERE` deja el indice inservible; `>= @PeriodStart AND < @PeriodEnd` lo usa.
- **Los indices son parciales sobre los activos.** Toda lectura filtra `is_active`, asi que el
  indice no carga con las filas dadas de baja.

---

## Frontend: el store plano y la descarga en bloques

El error de arranque es guardar el arbol como arbol. Aqui el store es plano:

```js
{ nodesById: {}, childIdsById: {}, pageStateById: {} }
```

- `childIdsById[id] === undefined` es **sin cargar**; `=== []` es **cargado y vacio**. Confundirlas
  produce el nodo que gira para siempre, o el que vuelve a pedir cada vez que se abre.
- **Loading y error por nodo.** Una rama que falla muestra su error en su renglon y las demas
  siguen navegables. Un spinner global aqui es una regresion de producto.
- **Expandir consulta la cache antes de pedir; colapsar no descarta.** La cache vive lo que vive
  la busqueda: al cambiar los parametros, el store se reinicia entero.
- **Guardia de cancelacion** en el efecto de la raiz (`AbortController` mas bandera `active`) y
  **dedup** al cargar mas paginas, para que un id repetido entre paginas no se dibuje dos veces.
- **Ingesta recursiva**, porque la hoja llega con su subarbol ya armado.

La descarga se acota al nodo seleccionado con **sus mismas coordenadas** (cero parametros
nuevos), baja en paginas de 20,000 con 4 en paralelo, y cada pagina **se textualiza al vuelo y
se acumula como bloque**. El `Blob` se arma del arreglo de partes, asi que el navegador une el
archivo sin materializar un string gigante en el heap. BOM UTF-8, `CRLF`, comillas duplicadas,
fecha en el nombre y `URL.revokeObjectURL` despues del clic.

Cancelar **no es un error**: si el usuario aborto a proposito, no se pinta nada en rojo.

### Accesibilidad e i18n

El arbol sigue el patron WAI-ARIA de treeview: `role="tree"`, `role="treeitem"`, `role="group"`,
`aria-expanded` solo donde hay algo que expandir, `aria-level`, `aria-selected`, y teclado
completo (Enter y Espacio seleccionan, Derecha expande, Izquierda colapsa, Arriba y Abajo mueven
el foco entre renglones visibles). Enlace de salto al contenido, `role="alert"` en los errores y
`aria-live` en el progreso de la descarga.

**Ningun texto visible esta incrustado.** Los diccionarios `es` y `en` tienen el mismo juego de
llaves. Una consecuencia de diseno que vale la pena notar: **el servidor manda datos, no texto
compuesto**. El nodo de mes trae `label: "7"` y `metrics.orderCount: 20423`; el nombre del mes lo
pone `Intl.DateTimeFormat` en el idioma activo y la frase "20,423 pedidos" la arma el cliente con
plural y separador de miles del locale. Lo mismo en la exportacion: el servidor manda llaves de
columna y valores, y el cliente traduce el encabezado y formatea numeros y fechas, para que el
archivo salga con los mismos formatos que la pantalla.

---

## Mapa del repositorio

```
lazy-hierarchy-orders/
  docker-compose.yml              PostgreSQL 16, con ajustes de dev para que sembrar sea rapido
  .env.example                    valores por defecto del compose
  backend/
    LazyHierarchy.slnx
    Common/                       mediador minimo + envelope (stand-in de la libreria compartida)
    Shared/
      LazyHierarchy.Kernel/       BaseEntity, interfaces marcadoras, contexto de tenant (sin deps)
      LazyHierarchy.Shared/       marcador de BD, wrapper Dapper, AppDbContext, TenantScope
      LazyHierarchy.Shared.Web/   BaseApiController y el middleware de claims
    Modules/Orders/
      Orders.Domain/              entidades, modelos del arbol, interfaces de repositorio
      Orders.Contracts/           tipos POCO publicos (unica via inter-modulo)
      Orders.Application/         Request / Handler / Responses
      Orders.Infrastructure/      OrdersTreeSql, Row, repositorios, configuraciones, migraciones, seeder
      Orders.Presentation/        controller y presenters
      Orders.Tests/               137 pruebas, incluida la del filtro de tenant
    LazyHierarchy.Host/           composicion, auth de ejemplo, .http con todos los casos
  frontend/
    src/api/                      cliente HTTP central y servicio del feature
    src/auth/                     token en memoria, nunca en localStorage
    src/i18n/                     es / en, plurales y formato por locale
    src/styles/designSystem.js    tokens unicos
    src/features/ordersTree/      pagina + hooks + componentes + utils
```

---

## Decisiones donde la especificacion no alcanzaba

- **La hoja devuelve dos grupos**, no una lista plana: las lineas del pedido y su resumen
  calculado, cada uno con sus hijos y todos con `nextLevel: null`. Sin algo anidado, la ingesta
  recursiva del cliente nunca se ejercitaria.
- **El nivel de fan-out no es la raiz**, a diferencia de la implementacion que inspira esto. Por
  eso la paginacion es **por nodo** (`pageStateById`) y no global, y "cargar mas" aparece dentro
  de la rama que pagina. El cliente sigue sin saber cual nivel pagina: se lo dice `hasMore`.
- **El nodo no trae texto compuesto**, trae dato y metricas. Es un cambio respecto al `subLabel`
  del patron original, y se hizo para que `rules/i18n-frontend` se pueda cumplir de verdad.
- **La tabla de pedidos se llama `customer_order`** porque `ORDER` es palabra reservada y
  nombrarla asi obligaria a entrecomillar el identificador en cada consulta, contra la decision
  de usar `snake_case` sin comillas.
- **`OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`** en todas las consultas paginadas, elegido sobre
  `LIMIT/OFFSET` por consistencia. El desplazamiento viaja como parametro (`@Offset`) en vez de
  calcularse dentro del SQL.
- **Sin hints de bloqueo.** `WITH (NOLOCK)` de la implementacion original no se traduce: se borra.
  PostgreSQL es MVCC y las lecturas no bloquean.
- **El emisor de tokens es andamiaje del ejemplo**, no parte del patron: `POST /api/session/token`
  entrega un JWT para el tenant que se pida, para poder comprobar el aislamiento en vivo. En un
  proyecto real ahi va un servicio de identidad completo. Lo que si es de verdad y hay que copiar
  es que el tenant viaja FIRMADO en el token.
- **`appsettings.Development.json` esta versionado con la cadena de conexion y la clave de firma
  de desarrollo**, a proposito, para que el ejemplo corra al clonarlo. En un proyecto real esos
  valores salen de variables de entorno o de un almacen de secretos, nunca del control de
  versiones.
- **El `AppDbContext` vive en la capa compartida** y recibe por inyeccion los ensamblados de los
  que toma las configuraciones. Con un solo modulo daria igual ponerlo dentro de `Orders`, pero
  asi el segundo modulo no obliga a mover nada.
- **`docker-compose.yml` trae `synchronous_commit=off`**, que cambia durabilidad por velocidad de
  siembra. En un ejemplo desechable es buen trato; en produccion no se hace.

## Lo que este ejemplo no incluye

- **Vista de grafo.** El alcance acordado es arbol mas exportacion masiva. El patron menciona
  cargar la vista cara en diferido con precarga en `onMouseEnter` y `onFocus`; sin segunda vista,
  aqui no hay nada que diferir.
- **Pre-agregacion de la raiz**, que es lo que hace falta de verdad si el nivel 1 agrega el
  universo (ver arriba).
- **Virtualizacion de la lista.** Con 50 renglones por pagina no hace falta; con miles abiertos a
  la vez, tocaria (`react-performance`).
