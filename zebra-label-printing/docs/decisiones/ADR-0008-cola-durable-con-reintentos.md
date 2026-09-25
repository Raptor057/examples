# ADR-0008 — Una cola durable con reintentos y espera creciente

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

Imprimir era una llamada sincrona: el cliente pedia la etiqueta, la API abria la impresora, y si la
impresora no contestaba devolvia el error. Ahi acababa todo.

Eso funciona mientras la impresora este encendida. En piso no lo esta la mitad del turno: se apaga
para cambiar el rollo, alguien desconecta el cable de red al mover una mesa, se queda sin ribbon a
medio lote. Y el resultado de la llamada sincrona es siempre el mismo: **la etiqueta se pierde**.
No se guarda en ningun lado, no se reintenta, y el operador se entera cuando la caja ya se fue sin
etiquetar.

Lo que hace daño no es el fallo: es que el fallo no deje rastro. Una impresora apagada un minuto se
arregla sola en cuanto alguien pasa y la enciende; lo que no se arregla es una etiqueta que ya nadie
sabe que hacia falta.

## Decision

Cuando un envio falla, el trabajo **se guarda** en `PrintQueueItem` y un despachador en segundo
plano lo reintenta con **espera creciente**, hasta **cinco intentos**.

Se guarda el **ZPL ya renderizado**, no la plantilla y sus valores.

## Por que una cola y no reintentar ahi mismo

Reintentar dentro de la peticion HTTP parece mas simple y es peor en todo:

- **Deja al cliente colgado.** Cinco intentos con espera razonable son minutos. Ningun cliente HTTP
  espera minutos, y el que lo intente se va a topar con el tiempo de espera de un proxy por el
  camino.
- **No sobrevive a nada.** Si el proceso se recicla —un despliegue, el reciclado del pool— los
  reintentos en memoria se evaporan con el, y la etiqueta vuelve a perderse.
- **No se puede mirar.** Nadie puede contestar "¿cuantas etiquetas van a salir cuando encienda esa
  impresora?", que es justo lo que se pregunta al volver de una caida.

Con la cola en base las tres se resuelven: el cliente recibe respuesta inmediata —y distinta, ver
abajo—, el trabajo sobrevive a un reinicio, y hay una tabla que mirar.

## Encolada NO es impresa, y la respuesta lo dice

Es el error clasico de una cola: el cliente da la etiqueta por puesta cuando sigue esperando. Por
eso hay **tres desenlaces**, no dos:

| Desenlace | Que paso | Que hace el cliente |
|---|---|---|
| `Printed` | Salio | Seguir |
| `Queued` | No salio, quedo esperando y se reintentara | Avisar al operador; **no** dar la caja por etiquetada |
| `Rejected` | No salio y no se guardo | Como antes: mostrar el error |

`Rejected` existe para no cambiarle el contrato a quien ya consumia el servicio: con
`queueOnFailure: false` el comportamiento es exactamente el de siempre.

## Por que el ZPL renderizado y no la plantilla con sus valores

Porque la plantilla puede cambiar mientras el trabajo espera. Si al reintentar se volviera a
renderizar, la etiqueta que sale seria la que la plantilla dice **media hora despues**, no la que se
pidio. En un lote donde la mitad salio antes de la caida, eso deja dos etiquetas distintas en cajas
del mismo pedido.

Cuesta espacio —el ZPL entero por trabajo— y se paga sin discutir.

## La espera creciente: 5 s, 30 s, 2 min, 8 min, 30 min

| Intento | Espera antes | Acumulado |
|---|---|---|
| 1 | 5 s | 5 s |
| 2 | 30 s | 35 s |
| 3 | 2 min | ~2.5 min |
| 4 | 8 min | ~11 min |
| 5 | 30 min | ~41 min |

**El primero va corto a proposito.** Cubre el caso mas comun con diferencia: un corte de red de un
instante, la impresora ocupada con el lote anterior. A los cinco segundos ya se resolvio solo, y una
espera larga ahi haria esperar un minuto a algo que estaba listo.

**De ahi en adelante crece**, porque el resto de las causas no son instantaneas. Una impresora
apagada, sin papel o desconectada necesita que **alguien pase y la atienda**, y eso tarda lo que
tarda una persona. Reintentar cada segundo no acerca la solucion: llena el log, mantiene ocupado al
despachador y castiga a la red con conexiones que van a fallar igual.

Es espera creciente **fija, sin aleatorizacion**. En un sistema con miles de clientes reintentando
contra el mismo servidor haria falta dispersar los intentos para no sincronizarlos; aqui los
destinos son impresoras distintas y el volumen es de piso, no de internet.

## El tope de cinco intentos

Cinco no es un numero magico: **con la espera de arriba son unos 41 minutos**, que es del orden de
lo que tarda alguien en notar que una impresora se apago y volver a encenderla.

Lo importante es el limite en si, no el numero. Reintentar indefinidamente parece generoso y es la
peor opcion: al encender la impresora **escupe de golpe las etiquetas de todo el turno**, ya
inservibles, pegadas a las buenas, y alguien tiene que separarlas a mano. Un trabajo que lleva horas
esperando ya no es una etiqueta pendiente: es basura que todavia no se sabe que lo es.

Al agotarlos, el trabajo pasa a **muerto** (`Status = 2`). Muerto significa exactamente eso: **no se
vuelve a intentar solo**. Se queda en la tabla con su ultimo error para que alguien lo mire y decida
—casi siempre, volver a pedir la etiqueta—, y se registra en la bitacora de impresion. Los
reintentos intermedios **no** se registran ahi: llenarian la bitacora de ruido y esconderian las
impresiones de verdad.

## Un destino ilegible se mata de una, sin reintentar

Si el JSON del destino no se puede deserializar, el trabajo se marca muerto en el primer intento en
vez de gastar los cinco. Insistir con algo que **ni siquiera se puede leer** no tiene ninguna
posibilidad de salir bien; lo unico que consigue es ocupar la cola cuarenta minutos.

## El reclamo, y que pasa cuando vence

El despachador **reclama** un lote de trabajos vencidos y los procesa. En este ejemplo el reclamo es
`SELECT ... WHERE Status = 0 AND NextAttemptAtUtc <= @Now ... LIMIT @Max`, se lee y despues se
marca.

> **Esto es lo que NO hay que copiar a un sistema con varias replicas.** Aqui vale porque SQLite es
> un archivo y el ejemplo corre en **un solo proceso**: no hay nadie mas leyendo. En produccion, con
> varias instancias de la API detras de un balanceador, leer primero y marcar despues abre una
> ventana en la que dos replicas toman el mismo trabajo y **la etiqueta sale dos veces**. Eso no es
> teorico: es el desenlace normal en cuanto hay dos replicas.
>
> La forma correcta es un **reclamo atomico**: una sola sentencia que selecciona, marca y devuelve
> bajo el mismo bloqueo (`UPDATE TOP (@n) ... OUTPUT INSERTED.* ... WITH (READPAST, UPDLOCK,
> ROWLOCK)` en SQL Server). `READPAST` hace que una replica se salte lo que otra tiene bloqueado en
> vez de esperarlo; `ROWLOCK` evita que el motor escale a bloqueo de tabla y las serialice a todas.

Y con reclamo atomico aparece el problema que lo acompaña: **una reserva abandonada**. Si una
replica se cae a media impresion, su trabajo se queda marcado como reclamado y nadie lo vuelve a
tomar nunca. Por eso el reclamo **caduca**: pasado un tiempo (cinco minutos es una eleccion
razonable) se considera abandonado y otra replica puede reclamarlo.

Ese plazo es un compromiso incomodo y conviene saberlo: **demasiado corto y se reimprime** una
etiqueta que la replica original todavia estaba mandando; **demasiado largo y el trabajo se queda
parado** sin que nadie lo atienda. Se elige largo —mas que el peor envio razonable— porque una
etiqueta duplicada es peor que una etiqueta tardia: la duplicada ya esta pegada en una caja.

## El despachador puede estar apagado, y se dice en voz alta

`PrintQueue:Enabled` lo apaga. Con la cola apagada los trabajos **se siguen encolando** pero nadie
los mueve, y eso pasado por alto se convierte en "las etiquetas no salen y no se por que". Por eso
el arranque lo registra como **advertencia**, no como informacion.

Ademas, una vuelta que revienta **no puede matar al despachador**: la excepcion se registra y se
sigue en la siguiente vuelta. Un servicio en segundo plano que se muere en silencio deja la cola
parada sin que nadie se entere.

## Donde vive cada cosa

- **La regla** —cuantos intentos y cuanto se espera— en `PrintQueueRules`, en Domain. Es pura: se
  prueba sin cola, sin base y sin reloj de verdad.
- **Que hacer en cada vuelta** en `PrintQueueWorker`, en Application. Se prueba sustituyendo los
  puertos, sin levantar nada.
- **Cada cuanto se llama** en `PrintQueueDispatcher`, en el Host. Eso es hospedaje, no negocio.

La separacion no es ceremonia: un `BackgroundService` con la logica adentro solo se puede probar
levantandolo.

## Lo que costo

- **Una tabla mas, y hay que mirarla.** Una cola que nadie revisa acumula trabajos muertos que
  nadie reimprime. Por eso `/health` publica cuantos hay pendientes y cuantos muertos, y hay una
  pantalla para verlos y cancelarlos.
- **El cliente tiene que distinguir tres desenlaces**, no dos. Es mas trabajo y es el punto: el
  desenlace ambiguo es el que rompe.
- **Espacio.** El ZPL renderizado de cada trabajo se guarda entero. `PrintQueueRules.KeepSent` y
  `KeepDead` acotan cuanto se conserva.

## Alternativas descartadas

- **Una cola de mensajes de verdad** (RabbitMQ, Service Bus). Resuelve todo esto mejor y trae un
  servicio mas que instalar, monitorear y explicar. Para un puñado de etiquetas por minuto contra
  impresoras de una planta, una tabla con un indice sobra.
- **Reintentar solo en memoria.** Gratis y no sobrevive a un reinicio, que es cuando mas falta hace.
- **Sin tope de intentos.** Descartado arriba: la impresora escupe el turno entero al encenderla.
