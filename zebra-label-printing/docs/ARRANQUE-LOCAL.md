# Levantarlo en local

Dos comandos y funciona **sin impresora**. Ese es el punto: si hiciera falta hardware, nadie
ejecutaria este ejemplo.

---

## Lo que hace falta

| | |
|---|---|
| .NET SDK | **10.0** o superior |
| Node | **20** o superior |
| Sistema operativo | **Windows** |
| Impresora | **Ninguna** para el modo por omision |

> **Por que Windows, sin rodeos.** El SDK de Link-OS solo publica para Windows, Android e iOS. No
> hay build para Linux ni macOS, asi que `Printing.Infrastructure` y el Host apuntan a
> `net10.0-windows10.0.26100`. No es una decision de este ejemplo: es el catalogo de Zebra.
>
> Todo lo demas —Domain, Application, las pruebas— apunta a `net10.0` a secas y compila donde sea.
> La dependencia de plataforma esta acorralada en dos proyectos, a proposito.

La primera compilacion baja el SDK de Zebra (~80 MB) y el paquete de referencia de Windows.

---

## Backend

```bash
cd backend
dotnet run --project LabelPrinting.Host
```

Queda en `http://localhost:5280`. Al arrancar aplica el esquema y siembra seis plantillas de
ejemplo; es idempotente, asi que arrancar dos veces no duplica nada.

Comprueba que vive, y **con que adaptador**:

```bash
curl http://localhost:5280/health
# {"status":"ok","printerDriver":"simulator","utcTimeStamp":"..."}
```

## Frontend

```bash
cd frontend
npm install
npm run dev
```

Queda en `http://localhost:5173`, con el proxy de Vite apuntando al backend. El codigo del cliente
**no conoce ninguna IP ni puerto**: pega a rutas relativas.

---

## Imprimir sin impresora

Por omision el adaptador es el **simulador**: cada etiqueta se guarda como `.zpl` en
`backend/LabelPrinting.Host/printed-labels/`.

```bash
curl -X POST http://localhost:5280/api/print/template \
  -H "Content-Type: application/json" \
  -d '{"code":"BOX_LABEL","dpi":203,
       "target":{"transport":"installed","queueName":"SIMULADOR-203"},
       "values":{"PART_NUMBER":"P-1001","SERIAL":"GT-000123","QUANTITY":"24"}}'
```

El archivo que aparece trae el ZPL **real**. Pegalo en
[Labelary](http://labelary.com/viewer.html) y ves la etiqueta dibujada, sin gastar una sola.

---

## Imprimir de verdad

En `backend/LabelPrinting.Host/appsettings.json`:

```json
{ "Printing": { "Driver": "zebra" } }
```

Reinicia. `/health` pasa a decir `"printerDriver": "zebra"` y el banner ambar de la pantalla
desaparece.

Un valor mal escrito **revienta el arranque** en vez de caer al simulador en silencio. Es
deliberado: caer en silencio significaria creer que imprimes mientras llenas una carpeta.

### Toda la configuracion

| Clave | Por omision | Para que |
|---|---|---|
| `ConnectionStrings:LabelPrintingDb` | `Data Source=label-printing.db` | El archivo de SQLite |
| `Printing:Driver` | `simulator` | `simulator` o `zebra` |
| `Printing:SimulatorOutputPath` | `printed-labels` | Donde deja los `.zpl` el simulador |
| `Printing:NetworkTimeoutMs` | `3000` | Espera al abrir o leer de una impresora de red |
| `Printing:DiscoveryTimeoutSeconds` | `5` | Cuanto dura el barrido de red |
| `Cors:AllowedOrigins` | `http://localhost:5173` | De donde se acepta al cliente |

Sobre `NetworkTimeoutMs`: los tiempos del SDK son generosos y dejan la peticion HTTP colgada mucho
mas de lo razonable. En piso, una impresora que no contesta en tres segundos esta apagada.

---

## Pruebas

```bash
cd backend
dotnet test LabelPrinting.slnx
```

32 pruebas, todas sin base ni impresora: las reglas son puras y los puertos se sustituyen.

---

## Si algo no arranca

**`NETSDK1206` al compilar.** El paquete de Zebra arrastra `Microsoft.WindowsAppSDK` con
identificadores de runtime antiguos. Ya esta acotado con `<NoWarn>` en los dos proyectos que
referencian el SDK. Si aparece en otro, es que alguien movio una referencia de sitio.

**`NU1903`, vulnerabilidad conocida.** Un paquete transitivo subio de version y el pin se quedo
corto. **No lo silencies**: busca la version parcheada y actualiza el pin. Ver
[ADR-0005](decisiones/ADR-0005-sqlite-y-esquema-al-arrancar.md).

**El puerto 5280 ocupado.** `dotnet run --project LabelPrinting.Host --urls http://localhost:OTRO`,
y ajusta `VITE_DEV_API_TARGET` para el cliente.

**La base quedo rara.** Borra `backend/LabelPrinting.Host/bin/Debug/.../label-printing.db` y
vuelve a arrancar: los scripts la reconstruyen.
