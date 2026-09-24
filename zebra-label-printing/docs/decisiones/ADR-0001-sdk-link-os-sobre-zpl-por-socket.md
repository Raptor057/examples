# ADR-0001 — Usar el SDK de Link-OS y no escribir al socket 9100 a pelo

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

Una impresora Zebra en red escucha ZPL en el puerto 9100. Mandarle una etiqueta es, literalmente,
abrir un socket TCP y escribir texto. Eso son unas quince lineas sin dependencias, funciona en
cualquier sistema operativo y no obliga a aceptar la licencia de nadie.

El SDK de Link-OS, en cambio, pesa, arrastra media docena de dependencias (SkiaSharp,
System.Management, FluentFTP, SNMP), **solo publica para Windows, Android e iOS**, y su licencia
pide aceptacion.

## Decision

Se usa el SDK.

## Por que

El socket crudo sirve para **mandar** y para nada mas. En cuanto el sistema tiene que responder
preguntas de piso, se acaba:

- **¿La impresora esta lista, o sin papel, o con el cabezal abierto?** Por socket no hay forma
  decente: hay que mandar comandos de estado y parsear su respuesta, que cambia entre familias de
  impresora. El SDK lo da tipado y trae el texto legible ya armado.
- **¿Que impresoras hay?** El descubrimiento en red y la enumeracion de Zebras instaladas son del
  SDK. A mano es reimplementar un protocolo de descubrimiento.
- **¿Y las impresoras conectadas por USB o por driver?** Con socket, sencillamente no llegas.

Un envio que falla en silencio es el peor resultado posible: el operador cree que la etiqueta salio
y la caja se va sin ella. Poder preguntar el estado ANTES y DESPUES es la razon de todo esto.

## Lo que costo

- **El proyecto es Windows.** No se puede contenerizar en Linux ni correr en un agente de CI que no
  sea Windows. Es la consecuencia mas cara y no tiene vuelta: el SDK no publica para Linux.
- Hay que aceptar la licencia de Zebra y vivir con sus dependencias transitivas, incluidas las
  vulnerabilidades que arrastren (ver ADR-0005 sobre el pin de paquetes).
- Subir de version mayor **rompe la compilacion**: el objetivo de plataforma cambia y alguna clase
  deja de ser instanciable. Ver [`../LINK-OS-SDK.md`](../LINK-OS-SDK.md).

## Como se mitiga

Todo el SDK entra por **un solo archivo** detras de `IPrinterGateway` (ADR-0004). Si algun dia el
precio de Windows pesa mas que el estado de la impresora, se escribe un adaptador de socket crudo y
no se toca nada mas.
