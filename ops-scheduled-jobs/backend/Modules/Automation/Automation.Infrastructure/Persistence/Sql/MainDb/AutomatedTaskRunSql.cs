namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>SQL de la bitacora de corridas. Solo se agrega y se cierra; nunca se borra.</summary>
internal static class AutomatedTaskRunSql
{
    private const string RunColumns = """
                   [Id], [TaskCode], [StartedAtUtc], [FinishedAtUtc], [Status],
                   [ItemsProcessed], [ItemsFailed], [Message], [WindowFromUtc], [WindowToUtc],
                   [CursorBeforeUtc], [CursorAfterUtc], [TriggeredByUser], [DispatcherInstance]
        """;

    /// <summary>
    /// Abre la corrida en RUNNING y devuelve su Id. SCOPE_IDENTITY y no @@IDENTITY: el segundo
    /// devuelve el ultimo identity de la SESION, asi que un trigger en otra tabla lo falsearia.
    /// </summary>
    public const string Insert = """
        INSERT INTO [ops].[AutomatedTaskRun]
            ([TaskCode], [StartedAtUtc], [Status], [WindowFromUtc], [WindowToUtc],
             [CursorBeforeUtc], [TriggeredByUser], [DispatcherInstance],
             [UtcTimeStamp], [UtcTimeStampLastUpdate])
        VALUES
            (@TaskCode, @StartedAtUtc, N'RUNNING', @WindowFromUtc, @WindowToUtc,
             @CursorBeforeUtc, @TriggeredByUser, @DispatcherInstance,
             @NowUtc, @NowUtc);

        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
        """;

    public const string Finish = """
        UPDATE [ops].[AutomatedTaskRun]
        SET    [FinishedAtUtc]          = @FinishedAtUtc,
               [Status]                 = @Status,
               [ItemsProcessed]         = @ItemsProcessed,
               [ItemsFailed]            = @ItemsFailed,
               [Message]                = @Message,
               [CursorAfterUtc]         = @CursorAfterUtc,
               [UtcTimeStampLastUpdate] = @FinishedAtUtc
        WHERE  [Id] = @Id;
        """;

    /// <summary>
    /// Cierra las corridas que quedaron abiertas cuando su instancia murio. Sin esto, la
    /// bitacora acumula filas RUNNING eternas y quien la mire no sabra cuales son de verdad.
    /// </summary>
    public const string AbandonOpen = """
        UPDATE [ops].[AutomatedTaskRun]
        SET    [FinishedAtUtc]          = @NowUtc,
               [Status]                 = N'FAILED',
               [Message]                = @Message,
               [UtcTimeStampLastUpdate] = @NowUtc
        WHERE  [TaskCode]      = @TaskCode
          AND  [Status]        = N'RUNNING'
          AND  [FinishedAtUtc] IS NULL;
        """;

    /// <summary>
    /// La pagina de la bitacora.
    ///
    /// Dos cosas que no son opcionales:
    ///
    /// - **OFFSET/FETCH mas COUNT(1) OVER()**, nunca TOP. TOP no dice cuantas hay en total, asi
    ///   que la pantalla no puede pintar el paginador ni decir "23 de 148".
    /// - **ORDER BY unico**: StartedAtUtc DESC y ademas Id DESC. Sin la llave, dos corridas que
    ///   comparten instante pueden salir en distinto orden en dos consultas, y OFFSET/FETCH
    ///   entonces repite filas en una pagina y se salta otras. El sintoma es una bitacora en la
    ///   que faltan corridas, y nadie sospecha del ORDER BY.
    /// </summary>
    public const string SelectPage = $"""
        SELECT {RunColumns},
               COUNT(1) OVER() AS [TotalCount]
        FROM   [ops].[AutomatedTaskRun]
        WHERE  [IsActive] = 1
          AND  [TaskCode] = @TaskCode
        ORDER  BY [StartedAtUtc] DESC, [Id] DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    /// <summary>
    /// La ultima corrida de cada tarea, para la pantalla de lista.
    ///
    /// Se resuelve con ROW_NUMBER en vez de con N consultas: una por tarea funciona con tres
    /// tareas y es un problema con treinta.
    /// </summary>
    public const string SelectLatestPerTask = $"""
        WITH [ranked] AS
        (
            SELECT {RunColumns},
                   ROW_NUMBER() OVER (PARTITION BY [TaskCode] ORDER BY [StartedAtUtc] DESC, [Id] DESC) AS [RowNumber]
            FROM   [ops].[AutomatedTaskRun]
            WHERE  [IsActive] = 1
              AND  [TaskCode] IN @TaskCodes
        )
        SELECT {RunColumns}
        FROM   [ranked]
        WHERE  [RowNumber] = 1;
        """;

    /// <summary>Historial de siembra. Es andamiaje del ejemplo, no del patron.</summary>
    public const string InsertSeeded = """
        INSERT INTO [ops].[AutomatedTaskRun]
            ([TaskCode], [StartedAtUtc], [FinishedAtUtc], [Status], [ItemsProcessed], [ItemsFailed],
             [Message], [WindowFromUtc], [WindowToUtc], [CursorBeforeUtc], [CursorAfterUtc],
             [TriggeredByUser], [DispatcherInstance], [UtcTimeStamp], [UtcTimeStampLastUpdate])
        VALUES
            (@TaskCode, @StartedAtUtc, @FinishedAtUtc, @Status, @ItemsProcessed, @ItemsFailed,
             @Message, @WindowFromUtc, @WindowToUtc, @CursorBeforeUtc, @CursorAfterUtc,
             @TriggeredByUser, @DispatcherInstance, @StartedAtUtc, @FinishedAtUtc);
        """;

    public const string CountAll = """
        SELECT COUNT(1) FROM [ops].[AutomatedTaskRun];
        """;
}
