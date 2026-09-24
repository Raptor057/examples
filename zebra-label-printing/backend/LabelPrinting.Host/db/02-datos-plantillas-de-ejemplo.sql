-- 02 - Plantillas de ejemplo, para que el sistema arranque con algo que imprimir.
--
-- Cada codigo existe DOS veces, una por resolucion, y el ZPL de cada una es DISTINTO: las
-- coordenadas van en puntos, asi que a 300 dpi todo esta multiplicado por ~1.48 respecto a 203.
-- Esa duplicacion no es un descuido del ejemplo, es exactamente el problema que resuelve la llave
-- (Code, Dpi). Ver ADR-0003.
--
-- El ZPL de aqui se puede pegar tal cual en http://labelary.com/viewer.html para ver la etiqueta
-- sin impresora.

INSERT OR IGNORE INTO LabelTemplate (Code, Name, Description, Body, Dpi, IsActive, CreatedAtUtc)
VALUES
(
    'BOX_LABEL',
    'Etiqueta de caja',
    'Etiqueta basica de caja: parte, descripcion, cantidad y codigo de barras del numero de serie.',
    '^XA
^CI28
^FO30,30^A0N,34,34^FD{{PART_NUMBER}}^FS
^FO30,75^A0N,24,24^FD{{PART_DESCRIPTION}}^FS
^FO30,110^A0N,24,24^FDCant: {{QUANTITY}}^FS
^FO30,145^A0N,24,24^FDFecha: {{PRINT_DATE}}^FS
^FO30,185^BY2^BCN,70,Y,N,N^FD{{SERIAL}}^FS
^XZ',
    203, 1, '2026-01-01T00:00:00Z'
),
(
    'BOX_LABEL',
    'Etiqueta de caja',
    'Misma etiqueta dibujada para 300 dpi: las coordenadas van escaladas, no es una copia.',
    '^XA
^CI28
^FO44,44^A0N,50,50^FD{{PART_NUMBER}}^FS
^FO44,111^A0N,35,35^FD{{PART_DESCRIPTION}}^FS
^FO44,163^A0N,35,35^FDCant: {{QUANTITY}}^FS
^FO44,214^A0N,35,35^FDFecha: {{PRINT_DATE}}^FS
^FO44,273^BY3^BCN,103,Y,N,N^FD{{SERIAL}}^FS
^XZ',
    300, 1, '2026-01-01T00:00:00Z'
),
(
    'PALLET_LABEL',
    'Etiqueta de tarima',
    'Etiqueta grande de tarima, con matriz de datos y el numero de cajas que lleva.',
    '^XA
^CI28
^FO30,30^A0N,45,45^FDTARIMA {{PALLET_NO}}^FS
^FO30,90^A0N,28,28^FDParte: {{PART_NUMBER}}^FS
^FO30,130^A0N,28,28^FDCajas: {{BOX_COUNT}}^FS
^FO30,170^A0N,28,28^FDCliente: {{CUSTOMER}}^FS
^FO400,90^BXN,6,200^FD{{PALLET_NO}}^FS
^XZ',
    203, 1, '2026-01-01T00:00:00Z'
),
(
    'PALLET_LABEL',
    'Etiqueta de tarima',
    'La de tarima dibujada para 300 dpi.',
    '^XA
^CI28
^FO44,44^A0N,66,66^FDTARIMA {{PALLET_NO}}^FS
^FO44,133^A0N,41,41^FDParte: {{PART_NUMBER}}^FS
^FO44,192^A0N,41,41^FDCajas: {{BOX_COUNT}}^FS
^FO44,251^A0N,41,41^FDCliente: {{CUSTOMER}}^FS
^FO591,133^BXN,9,200^FD{{PALLET_NO}}^FS
^XZ',
    300, 1, '2026-01-01T00:00:00Z'
),
(
    'ERROR_LABEL',
    'Etiqueta de error',
    'La que sale cuando algo no cuadra. Existe para que el operador se lleve un papel con el motivo y no una etiqueta a medias.',
    '^XA
^CI28
^FO30,30^GB540,150,4^FS
^FO50,60^A0N,40,40^FDERROR^FS
^FO50,110^A0N,26,26^FD{{MESSAGE}}^FS
^XZ',
    203, 1, '2026-01-01T00:00:00Z'
),
(
    'ERROR_LABEL',
    'Etiqueta de error',
    'La de error dibujada para 300 dpi.',
    '^XA
^CI28
^FO44,44^GB798,222,6^FS
^FO74,89^A0N,59,59^FDERROR^FS
^FO74,163^A0N,38,38^FD{{MESSAGE}}^FS
^XZ',
    300, 1, '2026-01-01T00:00:00Z'
);
