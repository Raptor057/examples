-- 04 - Datos de trabajo: eventos de pedido. Es la FUENTE de la ventana de la tarea
-- refresh-order-summary. La tarea toma [cursor, ahora] sobre OccurredAtUtc.

IF OBJECT_ID(N'[ops].[OrderEvent]', N'U') IS NULL
BEGIN
    CREATE TABLE [ops].[OrderEvent]
    (
        [Id]                      BIGINT         IDENTITY(1,1) NOT NULL,
        [OccurredAtUtc]           DATETIME2(3)   NOT NULL,
        [OrderNumber]             NVARCHAR(30)   NOT NULL,
        [ChannelCode]             NVARCHAR(20)   NOT NULL,
        [Units]                   INT            NOT NULL,
        [Amount]                  DECIMAL(18,2)  NOT NULL,

        [IsActive]                BIT            NOT NULL CONSTRAINT [DF_OrderEvent_IsActive] DEFAULT (1),
        [UtcTimeStamp]            DATETIME2(3)   NOT NULL CONSTRAINT [DF_OrderEvent_UtcTimeStamp] DEFAULT (SYSUTCDATETIME()),
        [UtcTimeStampLastUpdate]  DATETIME2(3)   NOT NULL CONSTRAINT [DF_OrderEvent_UtcTimeStampLastUpdate] DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT [PK_OrderEvent] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END;
GO
