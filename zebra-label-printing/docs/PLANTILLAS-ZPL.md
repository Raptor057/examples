# Escribir una plantilla ZPL

Una plantilla es ZPL con marcadores. Nada mas.

```zpl
^XA
^CI28
^FO30,30^A0N,34,34^FD{{PART_NUMBER}}^FS
^FO30,75^A0N,24,24^FD{{PART_DESCRIPTION}}^FS
^FO30,185^BY2^BCN,70,Y,N,N^FD{{SERIAL}}^FS
^XZ
```

Al guardarla, la API extrae los marcadores y los devuelve en `placeholders`. **El formulario del
cliente se dibuja a partir de eso**: agregar `{{LOTE}}` al ZPL hace aparecer el campo sin tocar el
front ni desplegar nada.

---

## Reglas de los marcadores

| | |
|---|---|
| Sintaxis | `{{NOMBRE}}`, con letras, numeros y guion bajo |
| Espacios | `{{ SERIAL }}` vale igual que `{{SERIAL}}` |
| Mayusculas | No importan al mandar los valores: se normalizan en el servidor |
| Repetido | Un marcador puede salir varias veces; se sustituyen todas |
| **Sin valor** | Queda **vacio**, nunca con el texto del marcador |

Lo ultimo es deliberado: una caja con `{{SERIAL}}` impreso es **peor** que una con el hueco en
blanco, porque parece un dato y alguien lo captura.

Un marcador sin valor **no impide imprimir**. Sale en `missingValues` de la respuesta para que el
cliente lo muestre como advertencia. Negarse por un campo opcional dejaria la linea parada.

---

## Lo que la API valida al guardar

Todo responde 400 con el motivo exacto:

- Codigo, nombre y cuerpo obligatorios.
- `dpi` tiene que ser **203, 300 o 600**.
- **El cuerpo debe empezar con `^XA` y terminar con `^XZ`.**

Esa ultima no es formalismo. Sin el cierre, la impresora se queda esperando y **la siguiente
etiqueta sale pegada a esta o no sale**. Es un fallo que aparece en la etiqueta de otro.

---

## El escapado, y por que no es opcional

ZPL es un lenguaje de comandos: `^` abre uno y `~` cambia el caracter de control. Un valor que los
traiga **deja de ser dato y pasa a ser instruccion**.

Un numero de parte `A^B` no imprime "A^B": imprime "A" y despues intenta ejecutar `^B`.

Por eso todo valor pasa por el escapado antes de entrar a la plantilla:

| Caracter | Se convierte en |
|---|---|
| `^` | `_5E` |
| `~` | `_7E` |
| `\` | `_5C` |
| salto de linea | espacio |

**Se escapa el valor, nunca la plantilla**: la plantilla es codigo y tiene que poder usar comandos.
Ver [ADR-0007](decisiones/ADR-0007-escapar-valores-antes-de-meterlos-al-zpl.md).

---

## Las dos versiones de cada etiqueta

En ZPL **las coordenadas van en puntos**. Un `^FO30,30` cae a 3.7 mm del borde en 203 dpi y a
2.5 mm en 300. Nada escala solo.

Por eso la llave es `(codigo, resolucion)` y cada version tiene su propio ZPL, con las coordenadas
y los tamaños de fuente **redibujados**, no multiplicados a ojo:

```zpl
# 203 dpi
^FO30,30^A0N,34,34^FD{{PART_NUMBER}}^FS

# 300 dpi — factor ~1.48
^FO44,44^A0N,50,50^FD{{PART_NUMBER}}^FS
```

Los codigos de barras son lo delicado: el `^BY` (ancho de modulo) y la altura hay que ajustarlos, o
el codigo deja de leerse. Ver
[ADR-0003](decisiones/ADR-0003-llave-por-codigo-y-resolucion.md).

---

## Comandos que se usan en las plantillas de ejemplo

| Comando | Que hace |
|---|---|
| `^XA` / `^XZ` | Abre y cierra la etiqueta. Obligatorios |
| `^CI28` | UTF-8. **Sin esto los acentos y la ñ salen mal** |
| `^FO x,y` | Posiciona en puntos desde la esquina superior izquierda |
| `^A0N,alto,ancho` | Fuente escalable, sin rotar |
| `^FD ... ^FS` | El dato y su cierre |
| `^BY ancho` | Ancho de modulo del codigo de barras |
| `^BCN,alto,Y,N,N` | Code 128 |
| `^BXN,alto,calidad` | DataMatrix |
| `^GB ancho,alto,grosor` | Recuadro |

---

## Probarla sin gastar etiquetas

Con el simulador activo, cada impresion deja un `.zpl` en
`backend/LabelPrinting.Host/printed-labels/`. Pega el contenido en
[Labelary](http://labelary.com/viewer.html) y ves la etiqueta dibujada.

La pantalla del cliente tambien muestra el ZPL ya renderizado y enlaza directo a Labelary. Es una
**aproximacion**: el render de verdad lo hace el servidor, que ademas escapa. Sirve para el acomodo,
no para verificar el escapado.

---

## Las plantillas que trae el ejemplo

| Codigo | Resoluciones | Marcadores |
|---|---|---|
| `BOX_LABEL` | 203, 300 | `PART_NUMBER`, `PART_DESCRIPTION`, `QUANTITY`, `PRINT_DATE`, `SERIAL` |
| `PALLET_LABEL` | 203, 300 | `PALLET_NO`, `PART_NUMBER`, `BOX_COUNT`, `CUSTOMER` |
| `ERROR_LABEL` | 203, 300 | `MESSAGE` |

`ERROR_LABEL` existe por una razon de piso: cuando algo no cuadra, el operador se lleva un papel con
el motivo en vez de una etiqueta a medias.
