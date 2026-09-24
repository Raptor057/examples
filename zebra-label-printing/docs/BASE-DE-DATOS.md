# La base: dos tablas y sus scripts

El sistema guarda **dos cosas**: las plantillas y la bitacora de lo que se mando a imprimir. Las
impresoras no se guardan — se preguntan al sistema operativo o a la red cada vez, porque una lista
de impresoras en base se desincroniza el primer dia.

Motor: **SQLite en un archivo**, con Dapper. El porque y sus limites, en
[ADR-0005](decisiones/ADR-0005-sqlite-y-esquema-al-arrancar.md).

---

## Los scripts

Viven en `backend/LabelPrinting.Host/db/` y se aplican **en orden**, por eso van numerados:

| # | Archivo | Que hace |
|---|---|---|
| 01 | `01-tabla-LabelTemplate.sql` | Crea `LabelTemplate` y `PrintJob` con sus indices |
| 02 | `02-datos-plantillas-de-ejemplo.sql` | Seis plantillas: tres etiquetas x dos resoluciones |

> **En este ejemplo los corre el arranque de la API.** Es comodo y es justo lo que **no** se debe
> copiar a un proyecto real: ahi los aplica una persona. El ADR-0005 lo dice con esas palabras.

Los dos son **idempotentes** (`CREATE TABLE IF NOT EXISTS`, `INSERT OR IGNORE`): correrlos dos
veces no rompe nada ni duplica plantillas. Un script de esquema que solo funciona la primera vez es
una trampa para el siguiente que levante el proyecto.

---

## `LabelTemplate`

```sql
CREATE TABLE LabelTemplate
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Code          TEXT    NOT NULL,
    Name          TEXT    NOT NULL,
    Description   TEXT        NULL,
    Body          TEXT    NOT NULL,   -- el ZPL con sus {{MARCADORES}}
    Dpi           INTEGER NOT NULL,
    IsActive      INTEGER NOT NULL DEFAULT 1,
    CreatedAtUtc  TEXT    NOT NULL,
    UpdatedAtUtc  TEXT        NULL
);

CREATE UNIQUE INDEX UX_LabelTemplate_Code_Dpi ON LabelTemplate (Code, Dpi);
```

**Ese indice unico es la regla de negocio, escrita donde no se puede esquivar.** La llave es
`(Code, Dpi)`, no el codigo solo. Sin el indice, nada impide dos plantillas con el mismo codigo y
resolucion, y a la hora de imprimir gana la que salga primero — un fallo que aparece en planta, no
en desarrollo. Ver [ADR-0003](decisiones/ADR-0003-llave-por-codigo-y-resolucion.md).

**Nunca se borra una plantilla**: `IsActive = 0`. Hay etiquetas ya pegadas en cajas cuya bitacora la
referencia.

---

## `PrintJob`

```sql
CREATE TABLE PrintJob
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Target        TEXT    NOT NULL,
    TemplateCode  TEXT        NULL,   -- nulo cuando fue ZPL directo
    Dpi           INTEGER     NULL,
    Zpl           TEXT    NOT NULL,   -- lo que de verdad viajo al cabezal
    Succeeded     INTEGER NOT NULL,
    Error         TEXT        NULL,
    CreatedAtUtc  TEXT    NOT NULL
);
```

Guarda **el ZPL exacto que se envio**, no la plantilla y los valores por separado. Es deliberado:
cuando alguien dice "esa etiqueta salio mal", lo primero que hace falta es lo que de verdad se
mando. La plantilla pudo cambiar desde entonces.

**Los intentos fallidos tambien se guardan**, con su motivo. Son justo los que alguien va a buscar.

---

## Dos detalles de SQLite que muerden

### No tiene booleano ni fecha

Guarda 0/1 y texto. Dapper materializa un **record posicional** buscando un constructor que case
con los tipos del lector, asi que `PrintJobEntry(long, ..., bool, DateTime)` revienta:

```
A parameterless default constructor or one matching signature
(Int64 Id, ..., Int64 Succeeded, String CreatedAtUtc) is required
```

Solucion: `PrintJobRow` en Infrastructure habla el idioma de SQLite y convierte. **El modelo de
dominio no se afloja para que le cuadre a la base.**

`LabelTemplate` no lo necesita porque usa propiedades `init`: ahi Dapper mapea por nombre y
convierte valor por valor.

### El orden de la bitacora va por Id, no por fecha

```sql
ORDER BY Id DESC
LIMIT @Take;
```

Ordenar solo por `CreatedAtUtc` repetiria y se saltaria filas cuando dos trabajos caen en el mismo
milisegundo — que con un lote de etiquetas pasa **siempre**. El `Id` es autoincremental y unico, asi
que el orden es estable.

---

## Consultas de diagnostico

**Plantillas que existen en una sola resolucion.** Van a fallar el dia que alguien imprima en la
otra, y el error no aparece hasta ese momento:

```sql
SELECT Code,
       MAX(CASE WHEN Dpi = 203 THEN 1 ELSE 0 END) AS tiene203,
       MAX(CASE WHEN Dpi = 300 THEN 1 ELSE 0 END) AS tiene300
FROM LabelTemplate
WHERE IsActive = 1
GROUP BY Code
HAVING MIN(Dpi) = MAX(Dpi);
```

**Impresiones fallidas de hoy, con su motivo:**

```sql
SELECT CreatedAtUtc, Target, TemplateCode, Error
FROM PrintJob
WHERE Succeeded = 0
  AND CreatedAtUtc >= date('now')
ORDER BY Id DESC;
```

**Que se imprimio en una impresora concreta:**

```sql
SELECT Id, CreatedAtUtc, TemplateCode, Succeeded
FROM PrintJob
WHERE Target = '192.168.0.50:9100'
ORDER BY Id DESC
LIMIT 50;
```
