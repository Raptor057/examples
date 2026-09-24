# Endpoints

Nueve endpoints. Todos devuelven el mismo envelope, y el codigo HTTP acompaña a `isSuccess` en vez
de sustituirlo. El porque, en
[ADR-0006](decisiones/ADR-0006-envelope-y-presenter-por-caso-de-uso.md).

```json
{ "data": {}, "isSuccess": true, "message": null, "utcTimeStamp": "2026-09-24T18:20:00Z" }
```

| Codigo | Cuando |
|---|---|
| 200 | Salio bien |
| 400 | Los datos de entrada no sirven (destino incompleto, ZPL sin `^XA`, dpi no soportado) |
| 404 | No existe la plantilla para esa combinacion de codigo y resolucion |
| 409 | Conflicto de estado: la plantilla ya existe, **o la impresora no contesta** |

> **Por que una impresora apagada es 409 y no 500.** Que una impresora de piso no conteste es su
> estado normal la mitad del turno; no es una falla del servidor. Devolver 500 llenaria el monitoreo
> de alarmas por algo que solo significa "ve a encenderla".

---

## El destino de impresion

Aparece en varios cuerpos. Son dos formas, y solo se mandan los campos de la que aplique:

```json
{ "transport": "network", "address": "192.168.0.50", "port": 9100 }
```
```json
{ "transport": "installed", "queueName": "ZDesigner ZT230-300dpi ZPL" }
```

`port` es opcional: por omision 9100, el estandar de ZPL.

---

## Impresoras

### Listar

```
GET /api/printers?includeNetwork=false
```

Con `includeNetwork=true` barre la red ademas de las colas locales. **Tarda segundos** y manda
paquetes de descubrimiento a toda la subred; por eso no es el comportamiento por omision.

```json
{ "data": [ { "name": "ZDesigner ZT230", "transport": "installed", "address": null, "isDefault": false } ],
  "isSuccess": true }
```

### Estado

```
POST /api/printers/status
```

Cuerpo: el destino. Es POST aunque solo lea, porque el destino es un objeto y meterlo en la query
string obliga a inventar un formato y a escaparlo a mano.

```json
{ "data": {
    "target": "192.168.0.50:9100",
    "isReadyToPrint": true, "isPaperOut": false, "isHeadOpen": false, "isPaused": false,
    "isRibbonOut": false, "isReceiveBufferFull": false, "isHeadTooHot": false,
    "labelsRemainingInBatch": 0,
    "messages": "",
    "readAtUtc": "2026-09-24T18:20:00Z" },
  "isSuccess": true }
```

`messages` es el texto legible que arma el propio SDK. Sin el, cada cliente inventa su traduccion
de las banderas y ninguna coincide con la de al lado.

---

## Imprimir

### Una plantilla con sus valores — el endpoint principal

```
POST /api/print/template
```

```json
{
  "code": "BOX_LABEL",
  "dpi": 203,
  "target": { "transport": "installed", "queueName": "SIMULADOR-203" },
  "values": { "PART_NUMBER": "P-1001", "SERIAL": "GT-000123", "QUANTITY": "24" }
}
```

Las llaves de `values` **no distinguen mayusculas**: se normalizan en el servidor.

```json
{ "data": {
    "receipt": { "target": "SIMULADOR-203", "bytesSent": 214, "elapsedMs": 3,
                 "sentAtUtc": "2026-09-24T18:20:00Z" },
    "templateCode": "BOX_LABEL",
    "dpi": 203,
    "missingValues": ["PRINT_DATE"] },
  "isSuccess": true }
```

**`missingValues` es una advertencia, no un error.** La etiqueta **se imprimio**; esos marcadores
salieron en blanco. Negarse a imprimir por un campo opcional dejaria la linea parada, asi que se
imprime y se avisa. El cliente deberia mostrarlo distinto de un exito limpio.

### ZPL escrito a mano

```
POST /api/print/zpl
```

```json
{ "target": { "transport": "network", "address": "192.168.0.50" },
  "zpl": "^XA^FO50,50^A0N,40,40^FDPRUEBA^FS^XZ" }
```

Es la valvula de escape para diagnosticar sin plantillas.

### Bitacora

```
GET /api/print/jobs?take=50
```

Lo ultimo que se mando, **con su ZPL exacto**, incluidos los intentos fallidos y su motivo. `take`
se recorta a 200: sin tope, alguien pide cien mil y se trae el ZPL de cada uno.

---

## Plantillas

| Verbo y ruta | Que hace |
|---|---|
| `GET /api/templates?dpi=203&isActive=true` | Lista. Los dos filtros son opcionales |
| `GET /api/templates/{code}?dpi=203` | Una. **El dpi es obligatorio**: el codigo solo no identifica una plantilla |
| `POST /api/templates` | Crea. **409** si ya existe ese par |
| `PUT /api/templates/{code}` | Edita. **404** si no existe |
| `DELETE /api/templates/{code}?dpi=203` | Baja logica. Nunca borra |

Cuerpo de alta y edicion:

```json
{ "code": "BOX_LABEL", "name": "Etiqueta de caja", "description": "Opcional",
  "body": "^XA^FO30,30^A0N,34,34^FD{{PART_NUMBER}}^FS^XZ", "dpi": 203 }
```

La respuesta incluye `placeholders`: los marcadores que la API encontro en el cuerpo. **El
formulario del cliente se dibuja a partir de eso**, no de una lista de campos escrita en el front.
Agregar `{{LOTE}}` al ZPL hace aparecer el campo sin desplegar nada.

Validaciones del cuerpo, todas 400 con el motivo exacto:

- Codigo, nombre y cuerpo obligatorios.
- `dpi` tiene que ser 203, 300 o 600.
- **El ZPL debe empezar con `^XA` y terminar con `^XZ`.** Sin eso la impresora se queda esperando el
  cierre, y la siguiente etiqueta sale pegada a esta o no sale.

---

## Salud

```
GET /health
```

```json
{ "status": "ok", "printerDriver": "simulator", "utcTimeStamp": "2026-09-24T18:20:00Z" }
```

**`printerDriver` es lo importante.** Es la primera pregunta cuando alguien reporta que imprimio y
no salio nada: muchas veces la respuesta es que estaba en simulador.
