# ADR-0006 — Un envelope unico y un presenter por caso de uso

**Estado:** Aceptada · **Fecha:** 2026-09-24

## Contexto

Toda respuesta de la API sale con la misma forma:

```json
{ "data": {}, "isSuccess": true, "message": null, "utcTimeStamp": "2026-09-24T18:20:00Z" }
```

Y el controller **no la serializa**: despacha el caso de uso, PUBLICA la respuesta, y devuelve un
view model que llena un presenter registrado para el tipo base de esa respuesta.

## Por que el envelope

Un fallo de negocio esperado —"la impresora no contesta", "esa plantilla ya existe"— no es un error
del servidor. Con el envelope, el cliente tiene **un solo sitio** donde leer si algo salio bien y
**un solo sitio** donde sacar el mensaje, sin ramificar por codigo HTTP en cada llamada.

Los codigos HTTP siguen siendo correctos: 400 validacion, 404 no existe, 409 conflicto. El envelope
no los sustituye, los acompaña.

## Por que un presenter y no serializar en el controller

Para que el controller no decida **que** se expone. El handler devuelve el resultado del caso de
uso; el presenter decide que parte de eso sale al cliente. Poner esa proyeccion en el controller
mezcla transporte con presentacion y termina con controllers que saben demasiado.

## El costo, y es alto: un presenter que falta no lo caza nadie

Si un caso de uso no tiene presenter, su endpoint responde:

```json
{ "data": null, "isSuccess": false, "message": "", "utcTimeStamp": "..." }
```

Eso **no** es un error de negocio ni de SQL: son los valores iniciales del `ResultViewModel` porque
nadie lo toco. El proyecto compila sin una advertencia, las consultas corren perfecto, y el fallo
solo aparece **al llamar la API**.

Es el error mas caro de esta arquitectura. En el sistema del que sale este ejemplo se fue asi a
produccion: cuatro endpoints de un modulo nuevo devolviendo el envelope vacio.

## Como se mitiga aqui

1. **Todos los presenters en UN archivo** (`PrintingPresenters.cs`), para que se vea de un vistazo
   si falta el de un caso de uso.
2. **Todos registrados juntos**, en el mismo orden, en `ServiceCollectionEx`.
3. La regla escrita donde se tropieza: en el comentario de `BaseApiController`.

## Alternativas descartadas

- **Registrar presenters por escaneo de ensamblados.** Quita el olvido pero mete magia: un presenter
  mal nombrado deja de registrarse y el sintoma es el mismo, sin nada que leer.
- **Un presenter generico por convencion.** Funciona hasta el primer caso de uso que necesita
  proyectar distinto, y entonces hay dos mecanismos conviviendo.
