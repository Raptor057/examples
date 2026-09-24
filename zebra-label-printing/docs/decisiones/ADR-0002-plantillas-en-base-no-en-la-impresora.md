# ADR-0002 — Las plantillas viven en la base, no en la memoria de la impresora

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

ZPL permite guardar un formato DENTRO de la impresora y luego imprimirlo mandando solo los valores
(`FormatUtil.PrintStoredFormat`, `^DF` / `^XF`). Es el camino clasico y tiene una ventaja real:
por la red viajan treinta bytes en vez de la etiqueta entera.

La alternativa es guardar el ZPL completo en una tabla y mandarlo ya armado en cada impresion.

## Decision

Las plantillas viven en `LabelTemplate`, en la base, y se mandan completas.

## Por que

- **Cambiar una etiqueta no deberia requerir tocar impresoras.** Con el formato en la impresora,
  corregir una posicion obliga a recargar el formato en CADA equipo. Con veinte impresoras en
  planta, eso es una tarde y una lista de las que se quedaron sin actualizar.
- **Una plantilla nueva vale para todas las impresoras el mismo segundo.**
- **Se puede ver que se imprimio.** El ZPL exacto queda en la bitacora. Con formatos residentes,
  la bitacora guarda "imprimi el formato 3 con estos valores" y el formato 3 puede haber cambiado
  desde entonces.
- **Una impresora que se reemplaza llega vacia** y no hay que acordarse de recargarle nada.

## Lo que costo

- Cada impresion manda la etiqueta completa: cientos de bytes en vez de decenas. En una red de
  planta es irrelevante; por un enlace lento y con lotes grandes, dejaria de serlo.
- No se aprovecha la memoria de la impresora para nada.

## Cuando reconsiderarla

Si aparecen etiquetas con graficos pesados embebidos, o un sitio remoto con enlace malo e
impresion por lotes. En ese caso conviene lo hibrido: el grafico residente en la impresora y el
texto por plantilla.
