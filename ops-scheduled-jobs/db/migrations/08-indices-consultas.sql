-- 08 - Los indices que necesitan las consultas del modulo.
--
-- Van en un script aparte del DDL de las tablas a proposito: un indice se agrega, se cambia o
-- se retira sin tocar la definicion de la tabla, y asi el diff dice a que consulta sirve.
-- Toda consulta nueva trae su indice (skill data-migrations).
--
-- Casi todos son FILTRADOS sobre los activos: toda lectura del modulo filtra IsActive = 1, asi
-- que el indice no tiene por que cargar con las filas dadas de baja.

-- Sirve al reclamo atomico (AutomatedTaskSql.ClaimDue): busca las vencidas y no reclamadas.
-- El filtro del indice repite las dos condiciones constantes del WHERE; NextRunAtUtc es la
-- llave porque es la que ordena la busqueda por rango.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AutomatedTask_NextRunAtUtc_ClaimedAtUtc' AND object_id = OBJECT_ID(N'[ops].[AutomatedTask]'))
    CREATE INDEX [IX_AutomatedTask_NextRunAtUtc_ClaimedAtUtc]
        ON [ops].[AutomatedTask] ([NextRunAtUtc] ASC, [ClaimedAtUtc] ASC)
        WHERE [IsActive] = 1 AND [IsEnabled] = 1;
GO

-- Sirve al paginado de la bitacora (ORDER BY StartedAtUtc DESC, Id DESC) y a la busqueda de la
-- ultima corrida de cada tarea. El Id va en la llave porque el ORDER BY del paginado tiene que
-- ser UNICO: sin el, OFFSET/FETCH repite y omite filas cuando dos corridas comparten instante.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AutomatedTaskRun_TaskCode_StartedAtUtc_Id' AND object_id = OBJECT_ID(N'[ops].[AutomatedTaskRun]'))
    CREATE INDEX [IX_AutomatedTaskRun_TaskCode_StartedAtUtc_Id]
        ON [ops].[AutomatedTaskRun] ([TaskCode] ASC, [StartedAtUtc] DESC, [Id] DESC)
        WHERE [IsActive] = 1;
GO

-- Sirve a la ventana de refresh-order-summary: rango sobre OccurredAtUtc.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderEvent_OccurredAtUtc_Id' AND object_id = OBJECT_ID(N'[ops].[OrderEvent]'))
    CREATE INDEX [IX_OrderEvent_OccurredAtUtc_Id]
        ON [ops].[OrderEvent] ([OccurredAtUtc] ASC, [Id] ASC)
        WHERE [IsActive] = 1;
GO

-- Sirve a la ventana de purge-expired-temp-files: rango sobre ExpiresAtUtc.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TempFile_ExpiresAtUtc_Id' AND object_id = OBJECT_ID(N'[ops].[TempFile]'))
    CREATE INDEX [IX_TempFile_ExpiresAtUtc_Id]
        ON [ops].[TempFile] ([ExpiresAtUtc] ASC, [Id] ASC)
        WHERE [IsActive] = 1;
GO

-- Sirve a la ventana de retry-pending-dispatches: rango sobre QueuedAtUtc.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PendingDispatch_QueuedAtUtc_Id' AND object_id = OBJECT_ID(N'[ops].[PendingDispatch]'))
    CREATE INDEX [IX_PendingDispatch_QueuedAtUtc_Id]
        ON [ops].[PendingDispatch] ([QueuedAtUtc] ASC, [Id] ASC)
        WHERE [IsActive] = 1;
GO
