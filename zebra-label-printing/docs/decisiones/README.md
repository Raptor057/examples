# Decisiones de arquitectura (ADR)

Una decision por archivo, con su contexto, lo que se descarto y lo que costo. Son **inmutables**:
si una decision cambia, se escribe un ADR nuevo que reemplaza al viejo y el viejo pasa a
`Reemplazado por ADR-XXXX`. No se editan para que cuadren con el codigo de hoy.

Se abre un ADR cuando la decision es **cara de revertir** y **no se explica sola leyendo el
codigo**. Agregar un endpoint no lo amerita; elegir como se guarda una plantilla, si.

| # | Decision | Estado |
|---|---|---|
| [0001](ADR-0001-sdk-link-os-sobre-zpl-por-socket.md) | Usar el SDK de Link-OS y no escribir al socket 9100 a pelo | Aceptada |
| [0002](ADR-0002-plantillas-en-base-no-en-la-impresora.md) | Las plantillas viven en la base, no en la memoria de la impresora | Aceptada |
| [0003](ADR-0003-llave-por-codigo-y-resolucion.md) | La llave de una plantilla es (codigo, resolucion) | Aceptada |
| [0004](ADR-0004-puerto-de-impresion-con-simulador.md) | Un puerto de impresion con dos adaptadores, uno de ellos simulador | Aceptada |
| [0005](ADR-0005-sqlite-y-esquema-al-arrancar.md) | SQLite con Dapper, y el esquema se aplica al arrancar | Aceptada, **solo para el ejemplo** |
| [0006](ADR-0006-envelope-y-presenter-por-caso-de-uso.md) | Un envelope unico y un presenter por caso de uso | Aceptada |
| [0007](ADR-0007-escapar-valores-antes-de-meterlos-al-zpl.md) | Los valores se escapan antes de entrar al ZPL | Aceptada |
