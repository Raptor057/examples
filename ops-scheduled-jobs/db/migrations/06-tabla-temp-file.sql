-- 06 - Datos de trabajo de purge-expired-temp-files.
--
-- Purgar es dar de baja LOGICA (IsActive = 0), nunca DELETE fisico: regla de oro de
-- database-conventions. PurgedAtUtc dice cuando, y la fila sigue ahi para auditar.

IF OBJECT_ID(N'[ops].[TempFile]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[TempFile]
    (
        [Id]                      BIGINT         IDENTITY(1,1) NOT NULL,
        [FileName]                NVARCHAR(200)  NOT NULL,
        [SizeBytes]               BIGINT         NOT NULL,
        -- La tarea toma los que EXPIRARON dentro de la ventana. Si la tarea no corrio ayer,
        -- hoy se lleva dos dias de expiraciones y no se pierde ninguna.
        [ExpiresAtUtc]            DATETIME2(3)   NOT NULL,
        [PurgedAtUtc]             DATETIME2(3)   NULL,

        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_TempFile_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_TempFile_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_TempFile_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_TempFile] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO
