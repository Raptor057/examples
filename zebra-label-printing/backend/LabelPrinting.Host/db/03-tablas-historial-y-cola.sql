-- 03 - Historial de plantillas y cola de impresion.
--
-- Va en un script aparte y NUMERADO despues del 01 a proposito: asi se ve que son cosas que se
-- agregaron despues, y quien tenga la base creada solo corre este.

-- ---------------------------------------------------------------------------------------------
-- Historial de plantillas
--
-- Cada vez que se edita una plantilla, el cuerpo que se reemplaza cae aqui. Sirve para contestar
-- "con que etiqueta se imprimio esto en julio", que sin historial no tiene respuesta: la bitacora
-- dice BOX_LABEL y BOX_LABEL pudo cambiar diez veces desde entonces.
-- ---------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS LabelTemplateVersion
(
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Code          TEXT    NOT NULL,
    Dpi           INTEGER NOT NULL,
    Version       INTEGER NOT NULL,
    Name          TEXT    NOT NULL,
    Description   TEXT        NULL,
    Body          TEXT    NOT NULL,
    CreatedAtUtc  TEXT    NOT NULL,
    ReplacedAtUtc TEXT        NULL
);

-- Unico por (codigo, dpi, version): dos filas con la misma version serian dos verdades sobre el
-- mismo momento, y la bitacora que apunte a esa version dejaria de significar algo.
CREATE UNIQUE INDEX IF NOT EXISTS UX_LabelTemplateVersion_Code_Dpi_Version
    ON LabelTemplateVersion (Code, Dpi, Version);

-- ---------------------------------------------------------------------------------------------
-- Cola de impresion
--
-- Guarda el ZPL YA RENDERIZADO, no la plantilla y los valores: si la plantilla cambia mientras el
-- trabajo espera, la etiqueta que salga tiene que ser la que se pidio.
-- ---------------------------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS PrintQueueItem
(
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    TargetJson       TEXT    NOT NULL,
    TargetLabel      TEXT    NOT NULL,
    TemplateCode     TEXT        NULL,
    Dpi              INTEGER     NULL,
    Zpl              TEXT    NOT NULL,

    -- 0 pendiente, 1 enviado, 2 muerto (se agotaron los reintentos), 3 cancelado
    Status           INTEGER NOT NULL DEFAULT 0,
    Attempts         INTEGER NOT NULL DEFAULT 0,

    -- Texto ISO-8601 en UTC. SQLite compara texto, asi que el formato TIENE que ser ordenable
    -- alfabeticamente o la consulta de vencidos deja de funcionar.
    NextAttemptAtUtc TEXT    NOT NULL,
    LastError        TEXT        NULL,
    CreatedAtUtc     TEXT    NOT NULL,
    CompletedAtUtc   TEXT        NULL
);

-- El indice que sostiene al despachador: corre cada pocos segundos preguntando por vencidos.
-- Parcial sobre los pendientes, porque los enviados y los muertos no se consultan asi nunca.
CREATE INDEX IF NOT EXISTS IX_PrintQueueItem_Due
    ON PrintQueueItem (NextAttemptAtUtc) WHERE Status = 0;

-- Las dos columnas que la bitacora necesita (TemplateVersion y QueueItemId) van en el CREATE
-- TABLE del script 01, no aqui: SQLite no tiene ADD COLUMN IF NOT EXISTS y estos scripts se
-- aplican en CADA arranque, asi que un ALTER los volveria imposibles de repetir.
--
-- Si vienes de una base creada antes de este cambio, borra el archivo .db y deja que los
-- scripts la reconstruyan. Es un ejemplo: no hay nada que conservar.
