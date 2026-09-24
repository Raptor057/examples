# Impresion de etiquetas con Zebra Link-OS

Ejemplo ejecutable de una API de impresion de etiquetas: plantillas ZPL guardadas en base,
sustitucion de variables, envio a impresoras de red o instaladas, estado del equipo y bitacora de
lo impreso. Con cliente web.

**Corre sin impresora.** Por omision usa un adaptador simulador que guarda cada etiqueta como un
`.zpl` en disco, y ese ZPL es el real: se pega en [Labelary](http://labelary.com/viewer.html) y se
ve la etiqueta.

```bash
cd backend  && dotnet run --project LabelPrinting.Host   # http://localhost:5280
cd frontend && npm install && npm run dev                # http://localhost:5173
```

---

## Que enseña este ejemplo

No es "como mandar ZPL a una impresora" — eso son quince lineas. Es lo que rodea a esas quince
lineas cuando el sistema tiene que aguantar un turno:

| Problema real | Como se resuelve aqui |
|---|---|
| El SDK de la impresora solo corre en Windows y necesita hardware | Un **puerto** con dos adaptadores, uno de ellos simulador ([ADR-0004](docs/decisiones/ADR-0004-puerto-de-impresion-con-simulador.md)) |
| La misma etiqueta se ve distinta segun la impresora | La llave de una plantilla es **(codigo, resolucion)** ([ADR-0003](docs/decisiones/ADR-0003-llave-por-codigo-y-resolucion.md)) |
| Un numero de parte con `^` se convierte en comando | Los valores **se escapan** antes de entrar al ZPL ([ADR-0007](docs/decisiones/ADR-0007-escapar-valores-antes-de-meterlos-al-zpl.md)) |
| "Esa etiqueta salio mal" y nadie sabe que se mando | Bitacora con el **ZPL exacto**, incluidos los fallos |
| Falta un dato de la etiqueta | Se imprime **y se avisa**, en vez de parar la linea |
| Cambiar una etiqueta obliga a tocar veinte impresoras | Las plantillas viven en base ([ADR-0002](docs/decisiones/ADR-0002-plantillas-en-base-no-en-la-impresora.md)) |
| Alguien cree que imprime y esta en simulador | `/health` dice el adaptador, y la pantalla lo grita en ambar |

---

## La documentacion

| Documento | Cuando lo necesitas |
|---|---|
| [`docs/ARRANQUE-LOCAL.md`](docs/ARRANQUE-LOCAL.md) | Levantarlo, configurarlo, y que hacer si no arranca |
| [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md) | El mapa de capas, las tres costuras y el recorrido de una impresion |
| [`docs/ENDPOINTS.md`](docs/ENDPOINTS.md) | Los nueve endpoints con sus cuerpos y sus codigos |
| [`docs/PLANTILLAS-ZPL.md`](docs/PLANTILLAS-ZPL.md) | Escribir una plantilla: sintaxis, marcadores y escapado |
| [`docs/BASE-DE-DATOS.md`](docs/BASE-DE-DATOS.md) | Las dos tablas, los scripts y consultas de diagnostico |
| [`docs/LINK-OS-SDK.md`](docs/LINK-OS-SDK.md) | El SDK: que se usa, **que cambio de la 4 a la 5**, y sus trampas |
| [`docs/decisiones/`](docs/decisiones/) | Siete ADR con lo que se descarto y lo que costo |

---

## El stack

| | |
|---|---|
| Backend | .NET 10, Dapper, SQLite, mediador propio |
| SDK de impresion | **Zebra.Printer.SDK 5.0.3685** (Link-OS Multiplatform) |
| Frontend | React 19, Vite 7, Tailwind 4 |
| Pruebas | xUnit + NSubstitute |

**Requiere Windows** (`net10.0-windows10.0.26100`): el SDK de Zebra no publica para Linux ni macOS.
La dependencia esta acorralada en dos proyectos — Domain, Application y las pruebas apuntan a
`net10.0` a secas.

---

## Como esta partido

```
backend/
  Common/                      mediador, resultados y envelope
  Shared/                      acceso a datos y BaseApiController
  Modules/Printing/
    Printing.Domain/           entidades, PUERTOS y reglas puras
    Printing.Application/      casos de uso
    Printing.Infrastructure/   SQL + adaptador Zebra + adaptador simulador
    Printing.Presentation/     controllers y presenters
    Printing.Tests/            32 pruebas, sin base ni impresora
  LabelPrinting.Host/          composition root y scripts de esquema
frontend/                      pantalla unica: elegir, llenar, imprimir, ver
```

Todo el SDK de Zebra entra por **un solo archivo**:
[`ZebraLinkOsPrinterGateway.cs`](backend/Modules/Printing/Printing.Infrastructure/Printers/ZebraLinkOsPrinterGateway.cs).

---

## Lo que NO trae, dicho para que nadie lo copie creyendo que esta completo

- **Autenticacion y permisos.** Ninguno. Y ojo: el cuerpo de una plantilla es ZPL con todos sus
  comandos, asi que quien puede crear plantillas puede mandarle cualquier cosa a la impresora. En
  produccion eso tiene que ser un permiso aparte del de imprimir.
- **Cola y reintentos.** Si la impresora esta apagada, el trabajo se pierde y queda registrado. Un
  sistema de piso normalmente encola.
- **Aplicar el esquema a mano.** Aqui lo hace el arranque, por comodidad del ejemplo. En produccion
  eso es inaceptable y el [ADR-0005](docs/decisiones/ADR-0005-sqlite-y-esquema-al-arrancar.md) lo
  dice con esas palabras.
- **Concurrencia real.** SQLite en archivo, un proceso.
- **Multi-idioma.** Los textos del cliente estan en español, a pelo.
