-- 01 - La tabla de plantillas.
--
-- La llave de negocio es (Code, Dpi) y el indice unico es lo que lo hace cierto. Sin el, nada
-- impide dos plantillas con el mismo codigo para la misma resolucion, y a la hora de imprimir
-- gana la que salga primero. Ver ADR-0003.

CREATE TABLE IF NOT EXISTS LabelTemplate
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Code          TEXT    NOT NULL,
    Name          TEXT    NOT NULL,
    Description   TEXT        NULL,
    Body          TEXT    NOT NULL,
    Dpi           INTEGER NOT NULL,

    -- Version del cuerpo vigente. Arranca en 1 y sube en cada edicion; el cuerpo que se
    -- reemplaza cae en LabelTemplateVersion (script 03).
    Version       INTEGER NOT NULL DEFAULT 1,

    IsActive      INTEGER NOT NULL DEFAULT 1,
    CreatedAtUtc  TEXT    NOT NULL,
    UpdatedAtUtc  TEXT        NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_LabelTemplate_Code_Dpi ON LabelTemplate (Code, Dpi);

-- La bitacora de impresion. Guarda el ZPL EXACTO que se envio, que es lo primero que hace falta
-- cuando alguien dice "esa etiqueta salio mal": la plantilla y los valores por separado no
-- reconstruyen lo que de verdad viajo al cabezal.
CREATE TABLE IF NOT EXISTS PrintJob
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Target        TEXT    NOT NULL,
    TemplateCode  TEXT        NULL,
    Dpi           INTEGER     NULL,
    Zpl           TEXT    NOT NULL,
    Succeeded     INTEGER NOT NULL,
    Error         TEXT        NULL,
    CreatedAtUtc  TEXT    NOT NULL,

    -- Con que version de la plantilla se imprimio. Sin esto, saber que se uso "BOX_LABEL" no
    -- dice nada: esa plantilla pudo cambiar diez veces desde entonces.
    TemplateVersion INTEGER   NULL,

    -- De que trabajo de la cola salio, si es que paso por la cola.
    QueueItemId     INTEGER   NULL
);

CREATE INDEX IF NOT EXISTS IX_PrintJob_CreatedAtUtc ON PrintJob (CreatedAtUtc DESC);
