namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>SQL de los envios pendientes: la fuente de retry-pending-dispatches.</summary>
internal static class PendingDispatchSql
{
    /// <summary>
    /// Los envios de la ventana que siguen SIN RESOLVER.
    ///
    /// Solo PENDING: los SENT ya llegaron y los DEAD agotaron sus reintentos. Que los DEAD
    /// queden fuera es lo que permite que el cursor avance por encima de ellos; si un envio
    /// irrecuperable siguiera contando como pendiente, frenaria el cursor para siempre y la
    /// tarea se quedaria reprocesando el mismo dia hasta el fin de los tiempos.
    /// </summary>
    public const string SelectPendingInWindow = """
        SELECT TOP (@MaxItems)
               [Id], [Reference], [QueuedAtUtc], [Attempts], [SimulatedOutcome]
        FROM   [ops].[PendingDispatch]
        WHERE  [IsActive]     = 1
          AND  [Status]       = N'PENDING'
          AND  [QueuedAtUtc] >  @FromUtc
          AND  [QueuedAtUtc] <= @ToUtc
        ORDER  BY [QueuedAtUtc] ASC, [Id] ASC;
        """;

    public const string MarkSent = """
        UPDATE [ops].[PendingDispatch]
        SET    [Status]                 = N'SENT',
               [Attempts]               = @Attempts,
               [LastAttemptAtUtc]       = @AttemptedAtUtc,
               [LastError]              = NULL,
               [UtcTimeStampLastUpdate] = @AttemptedAtUtc
        WHERE  [Id] = @Id;
        """;

    /// <summary>
    /// El intento fallo. El estado nuevo lo decide el llamador: sigue PENDING si vale la pena
    /// reintentarlo, o pasa a DEAD si ya agoto los intentos.
    /// </summary>
    public const string MarkFailed = """
        UPDATE [ops].[PendingDispatch]
        SET    [Status]                 = @Status,
               [Attempts]               = @Attempts,
               [LastAttemptAtUtc]       = @AttemptedAtUtc,
               [LastError]              = @LastError,
               [UtcTimeStampLastUpdate] = @AttemptedAtUtc
        WHERE  [Id] = @Id;
        """;

    public const string InsertSeeded = """
        INSERT INTO [ops].[PendingDispatch]
            ([Reference], [QueuedAtUtc], [Status], [Attempts], [SimulatedOutcome],
             [UtcTimeStamp], [UtcTimeStampLastUpdate])
        VALUES
            (@Reference, @QueuedAtUtc, N'PENDING', 0, @SimulatedOutcome, @NowUtc, @NowUtc);
        """;

    public const string CountAll = """
        SELECT COUNT(1) FROM [ops].[PendingDispatch];
        """;
}
