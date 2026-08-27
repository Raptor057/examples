-- 05 - Destino de refresh-order-summary: el resumen por dia y canal.
--
-- La unicidad por (dia, canal) es lo que hace idempotente al MERGE de la tarea: reprocesar la
-- misma ventana dos veces deja el mismo resultado, y por eso el cursor puede quedarse corto sin
-- consecuencias.

IF OBJECT_ID(N'[ops].[OrderDailySummary]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[OrderDailySummary]
    (
        [Id]                      INT            IDENTITY(1,1) NOT NULL,
        [SummaryDate]             DATE           NOT NULL,
        [ChannelCode]             NVARCHAR(20)   NOT NULL,
        [OrderCount]              INT            NOT NULL,
        [Units]                   INT            NOT NULL,
        [Amount]                  DECIMAL(18,2)  NOT NULL,
        [RefreshedAtUtc]          DATETIME2(3)   NOT NULL,

        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_OrderDailySummary_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_OrderDailySummary_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_OrderDailySummary_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_OrderDailySummary] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OrderDailySummary_SummaryDate_ChannelCode' AND object_id = OBJECT_ID(N'[ops].[OrderDailySummary]'))
    CREATE UNIQUE INDEX [UX_OrderDailySummary_SummaryDate_ChannelCode]
        ON [ops].[OrderDailySummary] ([SummaryDate] ASC, [ChannelCode] ASC);
GO
