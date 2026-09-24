# ADR-0007 — Los valores se escapan antes de entrar al ZPL

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

Una plantilla trae marcadores `{{VARIABLE}}` y la API los sustituye por lo que mande el cliente. El
resultado se manda a la impresora como ZPL.

**ZPL es un lenguaje de comandos**, y el acento circunflejo (`^`) abre uno. La tilde (`~`) cambia el
caracter de control. Un valor que los traiga deja de ser dato y pasa a ser instruccion.

Un numero de parte llamado `A^B` no imprime "A^B": imprime "A" y despues intenta ejecutar el
comando `^B`. Con suerte la etiqueta sale rara; sin suerte, la impresora se queda esperando un
comando que nunca cierra y **la siguiente etiqueta tampoco sale**.

Es inyeccion, con otro nombre y sobre otro interprete.

## Decision

Todo valor pasa por `TemplateRules.EscapeForZpl` antes de entrar a la plantilla:

| Caracter | Se convierte en | Por que |
|---|---|---|
| `^` | `_5E` | Abre comando |
| `~` | `_7E` | Cambia el caracter de control |
| `\` | `_5C` | Escape del propio ZPL |
| salto de linea | espacio | Parte el campo `^FD` y corta la etiqueta |

La notacion `_XX` es el hexadecimal de ZPL: la unica forma de imprimir esos caracteres como texto.

## Dos decisiones que van con esta

- **Se escapa el VALOR, nunca la plantilla.** La plantilla es codigo y tiene que poder usar
  comandos; el valor es dato y no.
- **Un marcador sin valor queda VACIO**, jamas con el texto del marcador. Una caja con
  `{{SERIAL}}` impreso es peor que una con el hueco en blanco: parece un dato y alguien lo captura.

## Lo que costo

Un valor que legitimamente llevara un `^` se imprime escapado. No ha pasado en ningun numero de
parte real, y la alternativa —confiar— es inaceptable.

## Lo que este ADR NO cubre

Quien puede crear plantillas. El **cuerpo** de una plantilla es ZPL con todos sus comandos, asi que
quien la edita puede hacer lo que quiera con la impresora. En este ejemplo no hay autenticacion; en
produccion, dar de alta plantillas tiene que ser un permiso aparte del de imprimir.
