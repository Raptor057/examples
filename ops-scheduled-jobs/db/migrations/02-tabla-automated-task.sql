-- 02 - Definicion y estado de cada tarea automatizada. UNA fila por tarea.
--
-- Esta tabla guarda la CONFIGURACION, no el catalogo: que tareas existen lo decide el codigo
-- (AutomatedTaskCatalog). Aqui vive lo que se puede cambiar a las 3 de la manana sin compilar:
-- si esta prendida, cada cuanto corre, y hasta donde llego.

IF OBJECT_ID(N'[ops].[AutomatedTask]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[AutomatedTask]
    (
        [Id]                      INT            IDENTITY(1,1) NOT NULL,

        -- Identificador estable. Es la MISMA cadena que declara el catalogo en codigo; si una
        -- fila trae un Code que el codigo no conoce, el despachador ni la mira.
        [Code]                    NVARCHAR(80)   NOT NULL,

        -- Pausar sin desplegar. El despachador lo respeta, y "ejecutar ahora" tambien: si el
        -- boton se saltara la pausa, pausar dejaria de ser una garantia.
        [IsEnabled]               BIT            NOT NULL CONSTRAINT [DF_AutomatedTask_IsEnabled] DEFAULT (1),
        [PausedReason]            NVARCHAR(400)  NULL,

        -- Horario: INTERVAL (cada N minutos) o DAILY_AT (a estas horas LOCALES).
        -- RunAtLocalTimes se captura en hora local porque es como lo piensa quien opera; la
        -- conversion a UTC ocurre en UN solo lugar del codigo (SchedulingTimeZone).
        [ScheduleKind]            NVARCHAR(20)   NOT NULL,
        [IntervalMinutes]         INT            NULL,
        [RunAtLocalTimes]         NVARCHAR(200)  NULL,

        -- Para la pantalla: cuando corrio y cuando toca. Ambas en UTC, como toda columna de
        -- tiempo del esquema.
        [LastRunAtUtc]            DATETIME2(3)   NULL,
        [NextRunAtUtc]            DATETIME2(3)   NOT NULL,
        [LastStatus]              NVARCHAR(20)   NULL,

        -- EL CURSOR. Hasta donde se proceso BIEN. La ventana de la proxima corrida es
        -- [LastCutoffUtc, ahora], nunca un rango calculado a partir de la fecha actual.
        [LastCutoffUtc]           DATETIME2(3)   NOT NULL,

        -- El reclamo entre replicas. NULL = libre. Con valor = alguien la esta corriendo.
        -- ClaimedBy no es adorno: sin el, un reclamo vencido no dice quien se murio.
        [ClaimedAtUtc]            DATETIME2(3)   NULL,
        [ClaimedBy]               NVARCHAR(100)  NULL,

        -- Auditoria obligatoria (rules/database-conventions): soft delete + UTC siempre.
        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_AutomatedTask_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_AutomatedTask_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_AutomatedTask_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_AutomatedTask] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_AutomatedTask_ScheduleKind] CHECK ([ScheduleKind] IN (N'INTERVAL', N'DAILY_AT'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AutomatedTask_Code' AND object_id = OBJECT_ID(N'[ops].[AutomatedTask]'))
    CREATE UNIQUE INDEX [UX_AutomatedTask_Code] ON [ops].[AutomatedTask] ([Code] ASC);
GO
