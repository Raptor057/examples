# ADR-0004 — Un puerto de impresion con dos adaptadores, uno de ellos simulador

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

El SDK de Link-OS solo corre en Windows y, para hacer algo util, necesita una impresora Zebra
enfrente. Eso choca con tres situaciones normales:

- Alguien clona el repositorio para entender como funciona y no tiene una Zebra.
- Se desarrolla la aplicacion cliente y no hace falta gastar etiquetas en cada clic.
- Las pruebas automatizadas no pueden depender de hardware.

## Decision

El dominio declara el puerto `IPrinterGateway`. Hay **dos** adaptadores y la configuracion elige:

| `Printing:Driver` | Adaptador | Que hace |
|---|---|---|
| `zebra` | `ZebraLinkOsPrinterGateway` | El SDK de verdad |
| `simulator` (por omision) | `SimulatorPrinterGateway` | Guarda cada etiqueta como `.zpl` en una carpeta |

## Por que un simulador y no un mock de pruebas

Un mock vive en el proyecto de pruebas y no sirve para levantar la aplicacion. Este adaptador
**esta en Infrastructure a proposito**: es un adaptador de produccion como cualquier otro, solo que
su "impresora" es el sistema de archivos. Con el, el ejemplo se levanta y se recorre entero —incluso
la pantalla— sin hardware, y el ZPL que deja es **el real**: se pega en Labelary y se ve la etiqueta.

## Donde el simulador MIENTE, a proposito

`GetStatusAsync` siempre contesta "lista para imprimir". Podria fingir que se queda sin papel, y se
decidio que no: un simulador que inventa fallas esconde problemas reales detras de un estado
falso, y quien lo prueba deja de saber si lo que ve es su codigo o el teatro del simulador.

## El riesgo, y como se mitiga

El riesgo es obvio: **alguien cree que esta imprimiendo y esta llenando una carpeta.** Tres
medidas, todas en el codigo:

1. `/health` **dice que adaptador esta activo**. Es la primera pregunta cuando alguien reporta que
   imprimio y no salio nada.
2. La pantalla muestra un **banner ambar permanente** en modo simulador. No es un aviso que se
   cierra: esta siempre mientras el modo lo este.
3. Un valor mal escrito en `Printing:Driver` **revienta el arranque** en vez de caer al simulador
   en silencio. Caer en silencio seria exactamente el fallo que se quiere evitar.

## Lo que costo

Una capa de indireccion y dos implementaciones que mantener. A cambio: el proyecto se puede
recorrer sin hardware, las pruebas no dependen de una impresora, y cambiar de marca seria escribir
un tercer adaptador sin tocar la aplicacion.
