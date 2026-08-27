-- 07 - Datos de trabajo de retry-pending-dispatches: envios encolados hacia un sistema ajeno.
--
-- SimulatedOutcome es ANDAMIAJE DEL EJEMPLO, no parte del patron: sin un fallo reproducible no
-- se puede ensenar el resultado PARTIAL ni el freno del cursor. En un proyecto real la falla la
-- decide el sistema del otro lado.

IF OBJECT_ID(N'[ops].[PendingDispatch]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[PendingDispatch]
    (
        [Id]                      BIGINT         IDENTITY(1,1) NOT NULL,
        [Reference]               NVARCHAR(40)   NOT NULL,
        [QueuedAtUtc]             DATETIME2(3)   NOT NULL,

        -- PENDING (sin resolver) / SENT (entregado) / DEAD (agoto reintentos).
        -- PENDING es el unico estado que FRENA el cursor: mientras algo siga sin resolver, la
        -- ventana de la proxima corrida tiene que volver a incluirlo.
        [Status]                  NVARCHAR(20)   NOT NULL,
        [Attempts]                INT            NOT NULL CONSTRAINT [DF_PendingDispatch_Attempts] DEFAULT (0),
        [LastAttemptAtUtc]        DATETIME2(3)   NULL,
        [LastError]               NVARCHAR(400)  NULL,
        [SimulatedOutcome]        NVARCHAR(20)   NOT NULL,

        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_PendingDispatch_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_PendingDispatch_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_PendingDispatch_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_PendingDispatch] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_PendingDispatch_Status] CHECK ([Status] IN (N'PENDING', N'SENT', N'DEAD'))
    );
END;
GO
