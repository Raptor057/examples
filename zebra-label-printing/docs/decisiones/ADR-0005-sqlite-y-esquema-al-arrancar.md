# ADR-0005 — SQLite con Dapper, y el esquema se aplica al arrancar

**Estado:** Aceptada, **solo para el ejemplo** · **Fecha:** 2026-09-24

## Contexto

El proyecto necesita guardar plantillas y la bitacora de impresion. En produccion esto viviria en
SQL Server o PostgreSQL, con los scripts aplicados por una persona.

Pero un ejemplo que exige levantar un servidor de base de datos antes de ver nada, no se ejecuta:
se lee por encima y se cierra.

## Decision

**SQLite en un archivo, con Dapper**, y el esquema se aplica al arrancar corriendo en orden los
`.sql` de `backend/LabelPrinting.Host/db/`.

## Por que Dapper y no un ORM

Porque lo que este ejemplo tiene que enseñar es **donde vive el SQL** —en clases `*Sql`, separado
del repositorio— y **que va parametrizado siempre**. Un ORM escondería justo eso.

## Lo que NO se debe copiar de aqui

Esta es la parte importante del ADR, y por eso el estado dice "solo para el ejemplo":

> **Aplicar el esquema al arrancar es inaceptable en produccion.** El arranque de una API no
> deberia poder alterar el esquema: un despliegue accidental contra la base equivocada la modifica
> sin que nadie lo apruebe. En un proyecto de verdad los scripts van numerados en `db/` y **los
> corre una persona**, y `SchemaBootstrapper` no existe.

Lo mismo con SQLite: sirve para un ejemplo de un solo proceso. No tiene concurrencia de escritura
digna de ese nombre.

## Un detalle que muerde: SQLite no tiene ni booleano ni fecha

Guarda 0/1 y texto. Dapper materializa un **record posicional** buscando un constructor que case
con los tipos del lector, asi que contra `PrintJobEntry(long, ..., bool, DateTime)` falla con:

```
A parameterless default constructor or one matching signature
(Int64 Id, ..., Int64 Succeeded, String CreatedAtUtc) is required
```

La salida **no** es aflojar el modelo de dominio para que le cuadre a la base. Es un tipo de fila
en Infrastructure que habla el idioma de SQLite (`PrintJobRow`) y una conversion explicita. El
dominio no se entera de en que motor esta guardado.

`LabelTemplate` no necesita uno porque usa propiedades `init`: ahi Dapper mapea por nombre y
convierte valor por valor. El problema es exclusivo de los records posicionales.

## Sobre los paquetes pinados

`Microsoft.Data.Sqlite` arrastraba `SQLitePCLRaw` 2.1.11 y el paquete de OpenAPI arrastraba
`Microsoft.OpenApi` 2.0.0, **las dos con vulnerabilidad conocida** (NU1903). Con
`TreatWarningsAsErrors` eso tumba la compilacion, que es exactamente lo que debe pasar.

Se resolvio **pinando las transitivas a versiones parcheadas**, no silenciando el aviso: una
referencia directa gana sobre la transitiva. Los pines llevan comentario y se quitan cuando el
paquete de arriba traiga una version sana. Silenciar NU1903 seria enseñar a ignorar avisos de
seguridad.
