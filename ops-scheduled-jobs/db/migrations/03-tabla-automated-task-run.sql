-- 03 - Bitacora de corridas. UNA fila por ejecucion, append-only.
--
-- Sin esto la tarea es una caja negra que falla en silencio hasta que alguien nota el descuadre
-- semanas despues. WindowFrom/To no son adorno: son lo que permite responder "y lo del martes,
-- quien lo proceso" sin adivinar.

IF OBJECT_ID(N'[ops].[AutomatedTaskRun]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[AutomatedTaskRun]
    (
        [Id]                      BIGINT         IDENTITY(1,1) NOT NULL,
        [TaskCode]                NVARCHAR(80)   NOT NULL,

        [StartedAtUtc]            DATETIME2(3)   NOT NULL,
        -- NULL mientras corre. Una fila RUNNING vieja es la senal visible de una corrida que
        -- se murio a media ejecucion.
        [FinishedAtUtc]           DATETIME2(3)   NULL,

        -- RUNNING / SUCCESS / PARTIAL / FAILED / SKIPPED.
        -- SKIPPED (no habia nada que hacer) y la AUSENCIA de fila (no corrio) son estados
        -- distintos, y el segundo es una falla.
        [Status]                  NVARCHAR(20)   NOT NULL,

        [ItemsProcessed]          INT            NOT NULL CONSTRAINT [DF_AutomatedTaskRun_ItemsProcessed] DEFAULT (0),
        [ItemsFailed]             INT            NOT NULL CONSTRAINT [DF_AutomatedTaskRun_ItemsFailed] DEFAULT (0),
        [Message]                 NVARCHAR(1000) NULL,

        -- La ventana que cubrio esta corrida, y el cursor antes y despues.
        -- CursorAfterUtc menor que WindowToUtc significa que algo quedo pendiente a proposito.
        [WindowFromUtc]           DATETIME2(3)   NOT NULL,
        [WindowToUtc]             DATETIME2(3)   NOT NULL,
        [CursorBeforeUtc]         DATETIME2(3)   NOT NULL,
        [CursorAfterUtc]          DATETIME2(3)   NULL,

        -- NULL = automatica. Con valor = alguien le dio a "ejecutar ahora".
        [TriggeredByUser]         NVARCHAR(100)  NULL,
        [DispatcherInstance]      NVARCHAR(100)  NULL,

        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_AutomatedTaskRun_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_AutomatedTaskRun_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_AutomatedTaskRun_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_AutomatedTaskRun] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO
