namespace Printing.Infrastructure.Persistence;

internal static class PrintJobSql
{
    public static string Insert => """
        INSERT INTO PrintJob
            (Target, TemplateCode, Dpi, Zpl, Succeeded, Error, CreatedAtUtc, TemplateVersion, QueueItemId)
        VALUES
            (@Target, @TemplateCode, @Dpi, @Zpl, @Succeeded, @Error, @CreatedAtUtc, @TemplateVersion, @QueueItemId);
        """;

    /// <summary>
    /// LIMIT con parametro y ORDER BY por Id descendente. El Id es autoincremental y unico, asi
    /// que el orden es estable: ordenar solo por fecha repetiria o se saltaria filas cuando dos
    /// trabajos caen en el mismo milisegundo, que con un lote de etiquetas pasa siempre.
    /// </summary>
    public static string Recent => """
        SELECT
            Id           AS Id,
            Target       AS Target,
            TemplateCode AS TemplateCode,
            Dpi          AS Dpi,
            Zpl          AS Zpl,
            Succeeded    AS Succeeded,
            Error           AS Error,
            CreatedAtUtc    AS CreatedAtUtc,
            TemplateVersion AS TemplateVersion,
            QueueItemId     AS QueueItemId
        FROM PrintJob
        ORDER BY Id DESC
        LIMIT @Take;
        """;
}
