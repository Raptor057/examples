# ADR-0003 — La llave de una plantilla es (codigo, resolucion)

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

En ZPL **las coordenadas van en puntos, no en milimetros**. Un `^FO30,30` cae a 3.7 mm del borde en
una impresora de 203 dpi y a 2.5 mm en una de 300. Nada escala solo: la misma etiqueta enviada a
las dos sale distinta, y en la de 300 dpi el contenido se amontona en una esquina.

Hay tres salidas posibles:

1. Una plantilla por codigo, y escalar las coordenadas al vuelo.
2. Una plantilla por codigo, y meter el dpi en el nombre (`BOX_LABEL_300`).
3. El codigo y la resolucion juntos como llave.

## Decision

La llave de negocio es **`(Code, Dpi)`**, con indice unico. La misma etiqueta existe tantas veces
como resoluciones se impriman, y cada version tiene su propio ZPL.

## Por que

- **Escalar al vuelo no funciona de verdad.** Se pueden multiplicar las coordenadas, pero no las
  fuentes ni el grosor de las barras del codigo, y un codigo de barras mal escalado deja de leerse.
  Un escalado que falla en el 5 % de las etiquetas es peor que no escalar.
- **Meter el dpi en el nombre es la misma llave, peor hecha.** Obliga a partir cadenas para saber
  que dos codigos son la misma etiqueta, y nada impide escribirlo mal.
- Con la llave compuesta, el error sale a la cara y temprano: pedir una plantilla para una
  resolucion que no existe responde **404 nombrando las dos partes** —"No existe BOX_LABEL para 300
  dpi"— y no un 404 pelado que manda a buscar el problema donde no esta.

## Lo que costo

- **Duplicacion real:** cada etiqueta se dibuja dos o tres veces, y un cambio de contenido hay que
  hacerlo en todas. Es el precio, y es consciente.
- Hay que acordarse de dar de alta las dos versiones. La consulta de diagnostico de
  [`../BASE-DE-DATOS.md`](../BASE-DE-DATOS.md) lista justo las que existen en una sola resolucion.

## Nota sobre el numero

Se usa **203**, no 200. El cabezal de 8 puntos/mm da 203.2 dpi y asi lo llama el catalogo de Zebra.
En planta se dice "200" por costumbre; el dato guardado dice la verdad.
