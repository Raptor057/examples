namespace Printing.Infrastructure.Persistence;

internal static class PrintQueueSql
{
    private const string Columns = """
                Id               AS Id,
                TargetJson       AS TargetJson,
                TargetLabel      AS TargetLabel,
                TemplateCode     AS TemplateCode,
                Dpi              AS Dpi,
                Zpl              AS Zpl,
                Status           AS Status,
                Attempts         AS Attempts,
                NextAttemptAtUtc AS NextAttemptAtUtc,
                LastError        AS LastError,
                CreatedAtUtc     AS CreatedAtUtc,
                CompletedAtUtc   AS CompletedAtUtc
        """;

    public static string Insert => """
        INSERT INTO PrintQueueItem
            (TargetJson, TargetLabel, TemplateCode, Dpi, Zpl, Status, Attempts, NextAttemptAtUtc, LastError, CreatedAtUtc)
        VALUES
            (@TargetJson, @TargetLabel, @TemplateCode, @Dpi, @Zpl, 0, @Attempts, @NextAttemptAtUtc, @LastError, @CreatedAtUtc)
        RETURNING Id;
        """;

    /// <summary>
    /// Los vencidos, del mas viejo al mas nuevo. `ORDER BY NextAttemptAtUtc, Id` y no solo por
    /// fecha: con varios trabajos programados al mismo segundo -que es lo normal tras una caida-
    /// el Id desempata y el orden deja de depender del humor del motor.
    ///
    /// En un motor con concurrencia real esto seria un claim atomico (UPDATE ... RETURNING con
    /// bloqueo). Con SQLite y un solo proceso basta con leer y marcar; el ADR-0008 lo dice.
    /// </summary>
    public static string ClaimDue => $"""
        SELECT
        {Columns}
        FROM PrintQueueItem
        WHERE Status = 0
          AND NextAttemptAtUtc <= @Now
        ORDER BY NextAttemptAtUtc, Id
        LIMIT @Max;
        """;

    public static string MarkSent => """
        UPDATE PrintQueueItem
        SET Status = 1, CompletedAtUtc = @Now, LastError = NULL
        WHERE Id = @Id AND Status = 0;
        """;

    public static string MarkFailed => """
        UPDATE PrintQueueItem
        SET Status           = CASE WHEN @Dead = 1 THEN 2 ELSE 0 END,
            Attempts         = @Attempts,
            LastError        = @Error,
            NextAttemptAtUtc = @NextAttemptAtUtc,
            CompletedAtUtc   = CASE WHEN @Dead = 1 THEN @NextAttemptAtUtc ELSE NULL END
        WHERE Id = @Id;
        """;

    /// <summary>Solo se cancela lo que sigue pendiente: cancelar algo ya impreso no significa nada.</summary>
    public static string Cancel => """
        UPDATE PrintQueueItem
        SET Status = 3, CompletedAtUtc = @Now
        WHERE Id = @Id AND Status = 0;
        """;

    public static string List => $"""
        SELECT
        {Columns}
        FROM PrintQueueItem
        WHERE (@Status IS NULL OR Status = @Status)
        ORDER BY Id DESC
        LIMIT @Take;
        """;

    public static string CountByStatus => """
        SELECT Status AS Status, COUNT(1) AS Total
        FROM PrintQueueItem
        GROUP BY Status;
        """;
}
