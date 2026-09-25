# ADR-0010 — Imprimir y administrar plantillas son permisos distintos

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

[ADR-0007](ADR-0007-escapar-valores-antes-de-meterlos-al-zpl.md) cerraba con una pregunta abierta:

> Quien puede crear plantillas. El **cuerpo** de una plantilla es ZPL con todos sus comandos, asi
> que quien la edita puede hacer lo que quiera con la impresora. En este ejemplo no hay
> autenticacion; en produccion, dar de alta plantillas tiene que ser un permiso aparte del de
> imprimir.

Este ADR la contesta. El ejemplo ya tiene autenticacion.

El detalle que lo hace necesario: **el escapado de ADR-0007 protege los valores, no la plantilla**.
Y eso es correcto —la plantilla es codigo y tiene que poder usar comandos—, pero significa que el
cuerpo de una plantilla es **una via directa a la impresora sin filtro ninguno**.

Lo que se puede hacer desde ahi no es "imprimir raro":

- **Reconfigurar la impresora.** `^JUS` guarda la configuracion actual como la de arranque: se puede
  dejar una impresora mal calibrada **de forma permanente**, y no se arregla apagandola.
- **Borrar su memoria.** `^ID*.*` borra los formatos y las fuentes almacenadas.
- **Dejarla colgada.** Un `^XA` sin su `^XZ` deja a la impresora esperando el cierre, y **la
  siguiente etiqueta —de otro— sale pegada o no sale**.
- **Imprimir cualquier cosa** con la identidad y en el papel de la planta.

Un operador de piso tiene que poder pulsar "imprimir" cien veces al turno. Eso no es el mismo nivel
de confianza.

## Decision

Tres permisos, declarados en `PermissionCatalog`:

| Permiso | Que abre |
|---|---|
| `printing:print` | Imprimir una plantilla con valores, listar plantillas e impresoras, ver su estado, ver la bitacora, vista previa |
| `printing:templates:manage` | Crear, editar y dar de baja plantillas — **y mandar ZPL crudo** |
| `printing:queue:manage` | Ver la cola y cancelar trabajos |

Y **mandar ZPL crudo exige `printing:templates:manage`**, no `printing:print`.

## Por que el ZPL crudo va con administrar y no con imprimir

Esta es la decision del ADR, y es la que menos se explica sola.

`POST /api/print/zpl` **parece** un endpoint de imprimir: esta bajo `/api/print`, se llama "imprimir
ZPL", y el controller entero declara `[Authorize(Policy = Print)]`. Es natural darle el permiso de
imprimir. Seria un error.

Lo que lo decide no es **el verbo**, es **que puede hacer**. Y mandar ZPL arbitrario puede hacer
exactamente lo mismo que guardar una plantilla con ese ZPL dentro y despues imprimirla: los mismos
comandos, la misma impresora, el mismo daño. **La unica diferencia es que no queda guardado** — lo
que, si acaso, lo hace peor, porque no deja rastro que auditar.

Si el ZPL crudo fuera del permiso de imprimir, el permiso de administrar plantillas **no protegeria
nada**: cualquiera con el de imprimir consigue el mismo efecto en una llamada, saltandose la
validacion de `^XA`/`^XZ`, el escapado y el historial de versiones.

> **La regla, para el siguiente endpoint que se agregue:** un endpoint se clasifica por **lo que
> permite hacer**, no por como se llama ni en que controller cayo. Si deja mandar contenido que la
> impresora va a interpretar como comandos, es administrar.

En el codigo el atributo del metodo **sobreescribe** al del controller, y lleva el comentario que
explica por que. Es el unico metodo de `PrintController` que lo hace, precisamente porque es el que
sorprende.

## Por que tres permisos y no dos, o cinco

**La cola es el tercero** porque cancelar trabajos no es imprimir ni es administrar plantillas: es
operacion. Y tiene consecuencia real —tras media hora de impresora caida, lo ultimo que alguien
quiere es que al encenderla escupa cuarenta etiquetas viejas—, asi que no puede ser anonimo. A la
vez, es lo que un supervisor de linea necesita sin darle acceso a editar el ZPL.

No hay mas porque cada permiso extra es uno que alguien tiene que conceder, y un catalogo con quince
permisos termina concedido entero "para que no moleste". Tres se pueden explicar en una frase cada
uno.

## El catalogo vive en codigo, no en una tabla

`PermissionCatalog` es una clase con constantes. Lo que va en base —en un sistema real— es **a quien
se le concede**, no cuales existen.

Un permiso que no existe en el codigo no protege nada: es una fila en una tabla que nadie consulta.
Tenerlos en codigo hace que la lista autoritativa sea la misma que el compilador verifica, y que un
permiso mal escrito en un `[Authorize]` sea un error de compilacion y no un endpoint que nunca deja
pasar a nadie.

Las politicas se registran **recorriendo el catalogo**, con el mismo nombre que el permiso. Asi el
endpoint escribe `[Authorize(Policy = PrintingPermissions.Print)]` y no hay dos listas que mantener
sincronizadas.

## Las constantes estan duplicadas a proposito

`PermissionCatalog` vive en el Host y `PrintingPermissions` en `Printing.Presentation`, con los
mismos tres valores.

No es un descuido. **Presentation no puede referenciar al Host**: seria una dependencia al reves, y
ademas ataria el modulo a un anfitrion concreto. Las alternativas eran peor: una cadena magica
repetida por los controllers, o un proyecto compartido nuevo para tres constantes.

El riesgo —que una deje de coincidir— existe y es acotado: el Host registra las politicas con **sus**
nombres, asi que un endpoint que pida una politica no registrada **falla al autorizar**, no deja
pasar en silencio.

## Lo que no declara politica, exige estar autenticado

La politica de reserva (`SetFallbackPolicy`) pide usuario autenticado. Es **lo contrario** de lo
habitual en ASP.NET Core, donde un endpoint sin atributo es anonimo.

Es deliberado: asi **un endpoint nuevo nace cerrado**. El fallo tipico del otro camino es que alguien
agrega un controller, olvida el `[Authorize]`, y queda abierto sin que nada lo señale — y el sintoma
es ninguno, porque funciona perfectamente.

`/health` y el emisor de tokens de desarrollo declaran `[AllowAnonymous]` explicitamente. Que sean
publicos es una decision escrita, no un olvido.

## La bandera: encender permisos sin romper a quien ya consume

`Access:EnforcePermissions` en `false` deja las politicas **declaradas pero no exigidas**: existen,
los endpoints las llevan puestas, y dejan pasar a todo el mundo.

Existe por una razon muy concreta, y no es comodidad. Encender autenticacion en un servicio que ya
tiene consumidores **los rompe a todos el mismo dia**, y no siempre se sabe quienes son: hay
clientes de escritorio instalados en maquinas de piso, tareas programadas, y algun script que
alguien escribio hace tres años. La bandera separa **desplegar el codigo** de **exigir el token**,
que son dos cosas que no tienen por que pasar a la vez.

El camino es: desplegar con la bandera apagada, ir migrando consumidores para que manden token,
comprobar que ya nadie llega sin el, y **entonces** encenderla.

> **El modo auditoria es un estado de transicion, no un destino.** Con la bandera apagada cualquiera
> puede crear plantillas, y una plantilla es ZPL con todos sus comandos. Un sistema que se queda
> asi "mientras tanto" durante un año tiene la seguridad escrita y ninguna aplicada.

Por eso el arranque **grita**: si la bandera esta apagada, se registra una advertencia que dice
exactamente que es lo que no se esta exigiendo. Y `/health` publica `permissionsEnforced`, para que
se pueda comprobar desde fuera en vez de adivinar.

En este ejemplo viene **encendida** (`true` en `appsettings.json`), porque aqui no hay consumidores
que romper y el valor por omision debe ser el seguro.

## El emisor de tokens del ejemplo no es un sistema de identidad

`DevTokenController` firma un token con los permisos que se le pidan, sin usuario ni contraseña.
Existe para poder **recorrer el ejemplo**, y solo se registra en Development: si alguien despliega
esto tal cual, el endpoint no esta.

En un sistema real el token lo emite el servicio de identidad del ecosistema y esta clase **no
existe**. Lo que si se conserva es el resto: la validacion del token, el catalogo y las politicas.

## Lo que costo

- **El ejemplo ya no se recorre sin pedir un token primero.** Es un paso mas en `ARRANQUE-LOCAL.md`,
  y es honesto: un ejemplo de impresion sin permisos enseñaria a montarlo sin permisos.
- **Hay que decidir el permiso de cada endpoint nuevo**, y la respuesta natural —"el del controller
  donde cayo"— es la equivocada en al menos un caso. Por eso la regla esta escrita arriba.
- **Las constantes duplicadas**, con su riesgo acotado.

## Alternativas descartadas

- **Un solo permiso para todo.** Es lo que hay hoy en muchos servicios de este tipo, y significa que
  el operador que imprime cajas puede reconfigurar la impresora. Se descarta justo por eso.
- **Roles en vez de permisos** (`operador`, `administrador`). Se lee bien hasta que alguien necesita
  media cosa —ver la cola sin editar plantillas— y hay que inventar un rol nuevo. Con permisos, se
  concede el que falta.
- **Validar el ZPL de las plantillas para impedir comandos peligrosos.** Una lista negra de comandos
  es una carrera que se pierde: `^JU`, `^ID`, `~JA`, y los que trae el siguiente firmware. Ademas
  impediria usos legitimos. **El control correcto es quien**, no que.
