namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>SQL de los eventos de pedido: la fuente de refresh-order-summary.</summary>
internal static class OrderEventSql
{
    /// <summary>
    /// La ventana, tal cual la define el patron.
    ///
    /// El limite inferior es EXCLUSIVO y el superior INCLUSIVO. No es un detalle de estilo: el
    /// cursor apunta al ultimo instante ya procesado, asi que incluirlo otra vez reprocesaria
    /// las filas de ese milisegundo en cada corrida. Con la pareja abierto/cerrado, cada fila
    /// entra exactamente una vez sin importar cuantas corridas haya en medio.
    ///
    /// El ORDER BY es parte del contrato: si las filas no llegan ordenadas por tiempo, "hasta
    /// donde salio bien" no significa nada.
    /// </summary>
    public const string SelectWindow = """
        SELECT TOP (@MaxItems)
               [Id], [OccurredAtUtc], [OrderNumber], [ChannelCode], [Units], [Amount]
        FROM   [ops].[OrderEvent]
        WHERE  [IsActive]       = 1
          AND  [OccurredAtUtc] >  @FromUtc
          AND  [OccurredAtUtc] <= @ToUtc
        ORDER  BY [OccurredAtUtc] ASC, [Id] ASC;
        """;

    public const string InsertSeeded = """
        INSERT INTO [ops].[OrderEvent]
            ([OccurredAtUtc], [OrderNumber], [ChannelCode], [Units], [Amount],
             [UtcTimeStamp], [UtcTimeStampLastUpdate])
        VALUES
            (@OccurredAtUtc, @OrderNumber, @ChannelCode, @Units, @Amount, @NowUtc, @NowUtc);
        """;

    public const string CountAll = """
        SELECT COUNT(1) FROM [ops].[OrderEvent];
        """;
}
