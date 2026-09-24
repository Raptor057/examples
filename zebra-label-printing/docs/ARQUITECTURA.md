# Arquitectura

Monolito modular por capas, con el SDK de la impresora detras de un puerto. La forma no es el
merito del ejemplo; el merito es **donde estan las costuras** y por que.

---

## El mapa

```
backend/
  Common/                        mediador minimo, resultados y envelope. Sin dependencias del dominio
  Shared/
    LabelPrinting.Shared/        acceso a datos (Dapper + SQLite) y arranque del esquema
    LabelPrinting.Shared.Web/    BaseApiController: despachar y traducir a HTTP
  Modules/Printing/
    Printing.Domain/             entidades, PUERTOS y reglas puras.        Sin dependencias
    Printing.Application/        casos de uso.                            Common + Domain
    Printing.Infrastructure/     SQL y los dos adaptadores de impresora.   Domain + Shared + SDK
    Printing.Presentation/       controllers y PRESENTERS.                Common + Application + Shared.Web
    Printing.Tests/              pruebas.                                 Application + Domain
  LabelPrinting.Host/            composition root. Ni una regla de negocio
frontend/                        React 19 + Vite + Tailwind
```

**La direccion de las flechas:**

```
Host --> Presentation --> Application --> Domain <-- Infrastructure
```

`Application` **nunca** referencia `Infrastructure`. Lo comprueba el compilador, porque la
referencia sencillamente no existe en el `.csproj`. Es lo que permite probar un caso de uso
sustituyendo sus puertos, sin base ni impresora.

---

## Las tres costuras que importan

### 1. El puerto de impresion

`IPrinterGateway` vive en Domain y **no menciona a Zebra**. El unico archivo de todo el proyecto que
la menciona es `ZebraLinkOsPrinterGateway`.

Eso compra dos cosas: el ejemplo corre con un simulador sin hardware, y cambiar de marca de
impresora seria escribir otro adaptador sin tocar la aplicacion. Ver
[ADR-0004](decisiones/ADR-0004-puerto-de-impresion-con-simulador.md).

### 2. El SQL, fuera de los repositorios

Todo el SQL vive en clases `*Sql`. Un repositorio las ejecuta; no las contiene. Asi se puede leer el
SQL del sistema entero abriendo una carpeta, y una consulta que hay que probar a mano se copia y se
pega sin desenredarla del codigo.

**Siempre parametrizado.** Ni una concatenacion con datos de entrada.

### 3. El presenter

El controller no serializa: publica la respuesta y el presenter llena el envelope. Es la costura
mas fragil del diseño —un presenter que falta no rompe la compilacion, rompe el endpoint en
ejecucion— y por eso todos viven en un archivo y se registran juntos. Ver
[ADR-0006](decisiones/ADR-0006-envelope-y-presenter-por-caso-de-uso.md).

---

## El recorrido de una impresion

Vale la pena seguirlo entero una vez:

1. **`PrintController.Template`** recibe el cuerpo y despacha `PrintTemplateRequest`.
2. **`PrintTemplateHandler`** —el unico sitio donde se juntan plantilla, valores e impresora—:
   - valida el destino (400 si esta incompleto);
   - busca la plantilla por `(codigo, dpi)` (404 si no esta);
   - normaliza las llaves de los valores a mayusculas;
   - calcula que marcadores faltan **antes** de imprimir, para poder avisarlo aunque salga bien;
   - **renderiza** con `TemplateRules.Render`, que escapa cada valor (ADR-0007);
   - manda por `IPrinterGateway` y **registra el resultado, salga bien o mal**.
3. **El adaptador** abre la conexion, escribe los bytes y la cierra tragandose el error de cierre.
4. **`PrintTemplatePresenter`** llena el envelope.
5. **`BaseApiController.MapResult`** elige el codigo HTTP segun la interfaz del resultado.

Fijate en el orden del paso 2: **el registro en bitacora ocurre en los dos caminos.** El intento
fallido es justo el que alguien va a buscar despues.

---

## Por que un mediador propio y no MediatR

Porque son cuarenta lineas y quitan una dependencia con licencia comercial. `Common/Messaging` hace
lo unico que este patron necesita: resolver el handler del contenedor y publicar la respuesta.

**Todos los handlers y presenters se registran a mano.** Nada de escaneo de ensamblados: un
registro que falta revienta con un mensaje que dice exactamente que interfaz no estaba, en vez de
desaparecer en una convencion magica.

---

## Lo que este ejemplo NO trae, y en produccion hace falta

Dicho para que nadie lo copie creyendo que esta completo:

- **Autenticacion y permisos.** No hay ninguno. Quien puede crear plantillas puede mandar cualquier
  comando a la impresora, porque el cuerpo de una plantilla es ZPL: eso tiene que ser un permiso
  aparte del de imprimir.
- **Concurrencia de verdad.** SQLite en archivo, un proceso.
- **Reintentos y cola.** Si la impresora esta apagada, el trabajo se pierde y se registra. Un
  sistema de piso normalmente encola y reintenta.
- **Metricas y trazas.** Hay registro estructurado y medicion de consultas lentas, nada mas.
- **Multi-idioma en el cliente.** Los textos estan en español, a pelo.
