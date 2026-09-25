# ADR-0009 — La vista previa la dibuja un servicio de terceros, y viene apagada

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

Escribir una plantilla ZPL a ciegas es caro. Las coordenadas van en puntos, nada escala solo entre
resoluciones, y el unico modo de saber si un codigo de barras cabe es imprimirlo. El ciclo real es:
editar, imprimir, mirar, corregir, imprimir otra vez. Cada vuelta gasta una etiqueta y obliga a
estar de pie junto a la impresora.

Poder **ver** la etiqueta dibujada antes de mandarla corta ese ciclo a segundos y no gasta papel.

El problema es que dibujar ZPL no es sustituir marcadores: es **interpretar un lenguaje de
impresora** —fuentes escalables, rotaciones, Code 128, DataMatrix, recuadros— y pintarlo en un mapa
de bits. Escribir eso es escribir un emulador de firmware de Zebra.

## Decision

La vista previa la resuelve **[Labelary](http://labelary.com)**, un servicio publico de terceros: se
le manda el ZPL por HTTP y devuelve un PNG.

Y viene **apagada por omision** (`Preview:Enabled = false`).

## Lo que se manda, dicho sin rodeos

Se manda **el ZPL completo ya renderizado**, con sus valores dentro. No es una plantilla en blanco:
es la etiqueta tal como saldria, y una etiqueta lleva **numero de parte, descripcion, cliente,
cantidad y numero de serie**.

Es decir: encender esta funcion con datos reales **saca datos de produccion de la red, a un servidor
que no controlamos**. Ni se cifra de forma especial, ni se anonimiza, ni hay acuerdo de por medio.

Eso es lo que hay que tener delante antes de encenderla. No es un detalle de configuracion.

## Lo que NO se debe mandar nunca

- **Datos de un cliente real** —numeros de parte, pedidos, cantidades, destinos—. Si la plantilla se
  esta ajustando, se ajusta con valores inventados: `P-0000`, `CLIENTE-DEMO`, `123456`.
- **Numeros de serie de produccion.** Identifican piezas concretas ya fabricadas.
- **Cualquier cosa que este cubierta por un acuerdo de confidencialidad** con el cliente al que va
  la caja. Muchos lo estan, y nadie firmo nada que permita mandarlo a un tercero.

Para lo que **si** sirve: acomodar una plantilla nueva, comprobar que un codigo de barras cabe,
comparar la version de 203 con la de 300 dpi. Todo eso se hace con datos falsos.

## Por que viene apagada por omision

Porque el valor por omision **es** la decision para el 95 % de las instalaciones. Nadie lee la
documentacion de una funcion que ya le funciona.

Si viniera encendida, alguien la usaria con datos reales el primer dia sin enterarse de que estaba
mandando etiquetas a internet — y el sintoma de esa fuga es **ninguno**: la vista previa se ve
bonita y todo parece bien. Un fallo de seguridad sin sintoma es el peor tipo.

Apagada, la unica forma de encenderla es que alguien **escriba la clave a mano**, y para escribirla
tiene que haber leido que hace. La friccion es el punto, no un descuido.

Cuando esta apagada el caso de uso devuelve un **409 con el motivo completo**, no un 404 ni un
silencio:

> La vista previa esta apagada. Se enciende con `Preview:Enabled`, y conviene leer antes por que
> esta apagada: manda el contenido de la etiqueta a un servicio de terceros.

El mensaje dice **como encenderla y por que no deberia encenderla a la ligera**, en la misma frase.
Un error que solo dice "no disponible" hace que el siguiente la encienda sin preguntar.

## Por que detras de un puerto, y no una llamada directa

`ILabelPreviewRenderer` esta en Domain; `LabelaryPreviewRenderer`, en Infrastructure. La llamada
HTTP a un tercero esta acorralada en **un solo archivo**.

Eso no es simetria por gusto: es la salida. El dia que la fuga no sea aceptable —o que Labelary
cambie su API, o deje de ser gratis, o se caiga— se escribe un renderizador local, se cambia el
registro en el contenedor, y **no se toca nada mas**. El caso de uso, el controller y la pantalla ni
se enteran.

`IsEnabled` esta en el puerto por la misma razon: que este disponible o no es una propiedad del
adaptador, y el caso de uso no tiene por que saber que la razon es una clave de configuracion de
Labelary.

## La vista previa usa el MISMO render que la impresion

`PreviewTemplateHandler` llama a `TemplateRules.Render`, exactamente el mismo que usa
`PrintTemplateHandler`, **con el mismo escapado** (ADR-0007).

Es deliberado y es importante: una vista previa hecha con una version "parecida" del render mentiria
justo en los casos raros —un valor con `^`, un acento, un marcador sin valor—, que son precisamente
los que se quieren ver antes de gastar una etiqueta. Una vista previa que no coincide con lo que
sale es peor que no tener vista previa, porque da confianza falsa.

## Es una imagen, y sale como imagen

El endpoint devuelve **el PNG directo**, no el envelope de ADR-0006. Es la unica excepcion de toda
la API, y esta razonada: meter una imagen en un campo JSON obliga a codificarla en base64, la
engorda un tercio, y obliga al cliente a decodificarla para volver a tener lo que ya tenia. Un
`<img src>` no puede consumir eso.

Los **fallos** de la vista previa si salen en el envelope, por la ruta normal: 404 si no existe la
plantilla, 409 si la vista previa no esta disponible. Solo el exito se sale del formato.

## El fallo es esperado, no es un 500

`PreviewUnavailableException` cubre los tres modos de fallo —apagada, el servicio no contesta, el
servicio rechazo el ZPL— y el caso de uso los convierte en **409**.

Ninguno es una falla del servidor. Que un servicio publico gratuito tarde o no conteste es su estado
normal; devolver 500 llenaria el monitoreo de alarmas por algo que no se puede arreglar y que no
impide imprimir.

Cuando Labelary **rechaza** el ZPL, su cuerpo de error dice **que comando no le gusto**, y ese texto
se devuelve recortado a 200 caracteres. Es justo lo que necesita leer quien esta escribiendo la
plantilla.

## Un detalle que muerde: Labelary mide en puntos por milimetro

La API lleva dpi (puntos por **pulgada**, que es como se habla de una Zebra) y Labelary espera dpmm:

| dpi | dpmm |
|---|---|
| 203 | `8dpmm` |
| 300 | `12dpmm` |
| 600 | `24dpmm` |

La conversion esta en el adaptador, donde pertenece. Mandar `203dpmm` no da error: **devuelve una
imagen absurda**, que es peor.

`Preview:LabelSize` (`4x6` por omision, en pulgadas) es el tamaño del papel que Labelary simula. No
sale del ZPL —el ZPL no lo declara— asi que hay que decirselo, y si no coincide con la etiqueta real
lo que se ve esta recortado o nadando en blanco.

## Lo que costo

- **Una dependencia externa** para una funcion del producto, aunque sea opcional y este acorralada.
- **La pantalla tiene que manejar que no este.** No puede asumir que el boton siempre funciona.
- **La documentacion tiene que insistir**, porque el riesgo es invisible cuando funciona. Por eso
  esta dicho en el puerto, en el adaptador, en el mensaje de error, en `ENDPOINTS.md` y aqui.

## Alternativas descartadas

- **Escribir un renderizador de ZPL local.** Es lo correcto a largo plazo y es un proyecto entero:
  interpretar fuentes, rotaciones y al menos tres simbologias de codigo de barras. No cabe en un
  ejemplo, y para un proyecto real hay que presupuestarlo como lo que es.
- **Levantar Labelary en un contenedor propio.** Resolveria la fuga sin escribir el renderizador y
  no existe: no publican imagen ni el codigo.
- **Enlazar al visor web de Labelary desde la pantalla** y que el usuario pegue el ZPL. Es lo que
  hace hoy la pantalla como aproximacion, y tiene el mismo problema de privacidad **sin ninguna
  barrera**: el usuario copia y pega sin que nadie lo apruebe. Al menos el endpoint tiene una clave
  de configuracion delante.
- **Dejarla encendida y avisar en la documentacion.** Descartado arriba: nadie lee la documentacion
  de lo que ya le funciona.
