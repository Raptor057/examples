# El SDK de Zebra (Link-OS Multiplatform)

Que parte del SDK usa este proyecto, que trae que aqui no se usa, **que cambio de la version 4 a la
5**, y las trampas que solo se descubren compilando.

**Fuente oficial:** [Link-OS Multiplatform SDK](https://www.zebra.com/la/es/support-downloads/software/printer-software/link-os-multiplatform-sdk.html)
— instalador, demos y documentacion por plataforma. Requiere cuenta Zebra.

> **De donde sale lo que dice este documento.** No de la pagina web: su tabla de descargas se arma
> por JavaScript y no se puede leer sin navegador. Sale de la **documentacion XML del paquete NuGet
> exacto** (`~/.nuget/packages/zebra.printer.sdk/<version>/lib/<tfm>/*.xml`) y de **compilar contra
> las dos versiones**. Todo se puede volver a comprobar ahi.

---

## Version que usa este ejemplo

```xml
<PackageReference Include="Zebra.Printer.SDK" Version="5.0.3685" />
```

| | |
|---|---|
| Plataformas que publica la 5.0.3685 | `net10.0-windows10.0.26100`, `net10.0-android36.0`, `net10.0-ios26.0` |
| Licencia | Archivo dentro del paquete, **requiere aceptacion** |
| Dependencias que arrastra | SkiaSharp, System.Drawing.Common, System.Management, FluentFTP, SharpSnmpLib, Newtonsoft.Json, BouncyCastle |

**No hay build para Linux ni macOS.** No es una omision del ejemplo: el SDK no lo publica. Por eso
`Printing.Infrastructure` y el Host apuntan a Windows, y por eso existe el simulador (ADR-0004).

---

## De la version 4 a la 5: que cambia de verdad

Esta es la seccion que justifica el documento. Los dos cambios de abajo **rompen la compilacion** y
ninguno esta anunciado en un CHANGELOG visible.

### 1. Cambia la plataforma objetivo: 19041 → 26100

| SDK | Objetivo que exige |
|---|---|
| `4.0.3435` | `net10.0-windows10.0.19041` |
| `5.0.3685` | **`net10.0-windows10.0.26100`** |

Subir de version obliga a **tocar todos los `.csproj` de la cadena**, no solo el que referencia el
paquete: el objetivo se contagia a todo lo que lo referencia, hasta el Host.

`10.0.26100` es el SDK de Windows 11 24H2. En una maquina que no lo tenga, el paquete de
referencia lo baja .NET solo; el problema aparece en agentes de compilacion con restauracion
bloqueada.

### 2. `PrinterStatus` paso a ser ABSTRACTA

El que muerde. En la 4.x el estado se leia asi, y era lo recomendado por ser mas barato:

```csharp
// SDK 4.x — ya NO compila en la 5
var status = new PrinterStatus(connection);
```

En la 5.x la clase es abstracta: sus concretas (`PrinterStatusZpl`, `PrinterStatusCpcl`) son
**internas** y no se pueden instanciar. El unico camino es la fabrica:

```csharp
// SDK 5.x
var printer = ZebraPrinterFactory.GetInstance(connection);
var status = printer.GetCurrentStatus();
```

**Lo que cuesta:** una consulta extra a la impresora, la que la fabrica hace para averiguar si
habla ZPL o CPCL. No hay forma de saltarsela, asi que leer el estado en la 5 es mas lento que en la
4. Si se leen varios valores, guardar una copia local del `PrinterStatus` deja de ser un consejo y
pasa a importar.

**Y trae una excepcion nueva que atrapar:** `ZebraPrinterLanguageUnknownException`, cuando la
fabrica no logra determinar el lenguaje. Pasa con cualquier cosa que no sea una Zebra escuchando en
el 9100 — mas comun de lo que parece, porque cualquier impresora de red acepta la conexion. Ver
`ZebraLinkOsPrinterGateway.GetStatusAsync`.

### 3. Lo que NO cambio

Comprobado tipo por tipo contra el XML de la 5.0.3685: siguen existiendo con la misma firma
`TcpConnection`, `DriverPrinterConnection`, `Connection.Open/Write/Close`,
`ZebraPrinterFactory.GetInstance` y `GetLinkOsPrinter`, `PrinterStatusMessages`, `SGD.GET/SET/DO`,
`UsbDiscoverer.GetZebraDriverPrinters`, `NetworkDiscoverer.FindPrinters`.

O sea: **la migracion 4 → 5 es el objetivo de plataforma y el estado.** El resto compila igual.

---

## Que usa este proyecto

**Un solo archivo**:
[`Printing.Infrastructure/Printers/ZebraLinkOsPrinterGateway.cs`](../backend/Modules/Printing/Printing.Infrastructure/Printers/ZebraLinkOsPrinterGateway.cs).
Todo lo demas habla con `IPrinterGateway` y no sabe que existe Zebra.

| Para | Se usa |
|---|---|
| Conectar por red | `new TcpConnection(host, puerto, timeoutApertura, timeoutLectura)` |
| Conectar a una cola instalada | `new DriverPrinterConnection(nombreDeLaCola)` |
| Mandar la etiqueta | `connection.Write(bytes)` |
| Leer el estado | `ZebraPrinterFactory.GetInstance(c).GetCurrentStatus()` |
| Texto legible del estado | `new PrinterStatusMessages(status).GetStatusMessage()` |
| Listar Zebras instaladas | `UsbDiscoverer.GetZebraDriverPrinters()` |
| Barrer la red | `NetworkDiscoverer.FindPrinters(handler)` |

Dos decisiones que se ven en el codigo y conviene entender:

- **`DriverPrinterConnection` en vez de mandar bytes por el spooler a mano** (P/Invoke a
  `winspool` con `pDataType = "RAW"`, que es lo habitual). Es una `Connection` normal, asi que una
  impresora instalada **tambien puede dar estado**. Con el spooler crudo, no.
- **`GetZebraDriverPrinters()` en vez de la lista de colas del sistema.** Devuelve solo Zebras. La
  lista del sistema trae el "Microsoft Print to PDF" y obliga al cliente a filtrar por nombre, a ojo.

---

## Lo que el SDK trae y aqui no se usa

| Clase | Que hace | Por que podria interesar |
|---|---|---|
| `SGD.GET/SET/DO` | Lee y escribe configuracion de la impresora | Preguntarle su **resolucion real** en vez de que el cliente la mande en cada peticion |
| `FormatUtil.PrintStoredFormat` | Imprime una plantilla guardada EN la impresora | Otra forma de resolver lo que aqui resuelve la tabla. Ver ADR-0002 |
| `GraphicsUtil` | Imprime imagenes | Logotipos sin meterlos como ZPL |
| `FileUtil`, `ToolsUtil`, `ProfileUtil` | Archivos, reinicio, calibracion, clonar configuracion | Administracion de flota |
| `ZebraPrinterLinkOs.GetPortStatus()` | Puertos TCP abiertos de la impresora | Diagnostico de red |
| `ConnectionBuilder.Build("TCP:192.168.0.50:9100")` | Conexion desde una cadena | Guardar destinos como texto en configuracion |
| `SnmpPrinter` | Consulta por SNMP | Monitoreo de flota sin abrir conexion de impresion |

---

## Trampas, todas comprobadas compilando

- **El SDK es SINCRONO.** No hay `OpenAsync` ni `WriteAsync`. De ahi que el adaptador envuelva
  todo en `Task.Run` con su `CancellationToken`: es la unica forma de no bloquear el hilo de la
  peticion. **No se quita.**
- **`Close()` tambien lanza `ConnectionException`.** Si el `finally` no se la traga, un fallo al
  cerrar tapa el error real del envio y el usuario lee "no se pudo cerrar la conexion" cuando lo
  que pasa es que la impresora esta apagada.
- **`NETSDK1206` tumba la compilacion con `TreatWarningsAsErrors`.** El paquete arrastra
  `Microsoft.WindowsAppSDK` con identificadores de runtime antiguos (`win10-x64`). No se puede
  arreglar desde aqui, asi que va un `<NoWarn>NETSDK1206</NoWarn>` acotado a los proyectos que
  referencian el SDK — no a la solucion entera.
- **El descubrimiento de red avisa por callback y no devuelve lista.** Hay que implementar
  `DiscoveryHandler` y esperar a `DiscoveryFinished` con un tiempo limite, o el metodo vuelve
  vacio. Ver `CollectingDiscoveryHandler`.
- **El SDK trae su propio `DiscoveredPrinter`** y choca con el del dominio. El adaptador usa alias
  (`DomainPrinter` / `SdkPrinter`) para que se vea cual es cual, que es justo su trabajo: traducir.
- **El puerto no tiene que ir a mano:** existe `TcpConnection.DEFAULT_ZPL_TCP_PORT`.

---

## Como comprobar todo esto sin creerme

```bash
# Que plataformas publica una version
curl -s https://api.nuget.org/v3-flatcontainer/zebra.printer.sdk/5.0.3685/zebra.printer.sdk.nuspec

# Que versiones existen
curl -s https://api.nuget.org/v3-flatcontainer/zebra.printer.sdk/index.json

# La firma exacta de cualquier tipo, en la version que tengas restaurada
#   ~/.nuget/packages/zebra.printer.sdk/<version>/lib/<tfm>/SdkApi.Core.xml
#   ~/.nuget/packages/zebra.printer.sdk/<version>/lib/<tfm>/SdkApi.Desktop.xml
```

Y para ver una etiqueta sin impresora, pega el ZPL en
[Labelary](http://labelary.com/viewer.html). El simulador de este ejemplo deja los `.zpl` listos
para eso.
