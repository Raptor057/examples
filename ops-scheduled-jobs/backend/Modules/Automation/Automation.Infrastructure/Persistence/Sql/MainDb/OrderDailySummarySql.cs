namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>SQL del resumen diario: el destino de refresh-order-summary.</summary>
internal static class OrderDailySummarySql
{
    /// <summary>
    /// Escritura IDEMPOTENTE por (dia, canal).
    ///
    /// Es lo que hace barato que el cursor se quede corto: si una corrida parcial deja el cursor
    /// antes del final de la ventana, la siguiente vuelve a pasar por esos dias y el resultado
    /// es identico. Sin idempotencia, "reprocesar por si acaso" duplicaria totales, y entonces
    /// la unica salida segura seria avanzar el cursor siempre, que es justo el error caro.
    ///
    /// MERGE necesita el punto y coma final: sin el, el motor lo rechaza.
    /// </summary>
    public const string Upsert = """
        MERGE [ops].[OrderDailySummary] WITH (HOLDLOCK) AS target
        USING (SELECT @SummaryDate AS [SummaryDate], @ChannelCode AS [ChannelCode]) AS source
            ON target.[SummaryDate] = source.[SummaryDate]
           AND target.[ChannelCode] = source.[ChannelCode]
        WHEN MATCHED THEN
            UPDATE SET target.[OrderCount]             = @OrderCount,
                       target.[Units]                  = @Units,
                       target.[Amount]                 = @Amount,
                       target.[RefreshedAtUtc]         = @RefreshedAtUtc,
                       target.[IsActive]               = 1,
                       target.[UtcTimeStampLastUpdate] = @RefreshedAtUtc
        WHEN NOT MATCHED THEN
            INSERT ([SummaryDate], [ChannelCode], [OrderCount], [Units], [Amount],
                    [RefreshedAtUtc], [UtcTimeStamp], [UtcTimeStampLastUpdate])
            VALUES (@SummaryDate, @ChannelCode, @OrderCount, @Units, @Amount,
                    @RefreshedAtUtc, @RefreshedAtUtc, @RefreshedAtUtc);
        """;
}
