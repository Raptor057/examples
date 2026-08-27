namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>SQL de los temporales: la fuente y el destino de purge-expired-temp-files.</summary>
internal static class TempFileSql
{
    /// <summary>
    /// Los que EXPIRARON dentro de la ventana. Mismo criterio de limites que el resto: inferior
    /// exclusivo, superior inclusivo.
    ///
    /// Que el filtro sea por fecha de expiracion y no por "todos los vencidos" es lo que hace
    /// que la tarea sea acotada: si estuvo pausada una semana, la corrida siguiente se lleva las
    /// expiraciones de esa semana y no reevalua la tabla entera.
    /// </summary>
    public const string SelectExpiredInWindow = """
        SELECT TOP (@MaxItems)
               [Id], [FileName], [SizeBytes], [ExpiresAtUtc]
        FROM   [ops].[TempFile]
        WHERE  [IsActive]      = 1
          AND  [ExpiresAtUtc] >  @FromUtc
          AND  [ExpiresAtUtc] <= @ToUtc
        ORDER  BY [ExpiresAtUtc] ASC, [Id] ASC;
        """;

    /// <summary>
    /// Purgar es dar de BAJA LOGICA. Nunca DELETE fisico: la regla de oro de
    /// database-conventions no tiene excepcion aqui solo porque la tabla se llame "temporal".
    /// </summary>
    public const string Purge = """
        UPDATE [ops].[TempFile]
        SET    [IsActive]               = 0,
               [PurgedAtUtc]            = @PurgedAtUtc,
               [UtcTimeStampLastUpdate] = @PurgedAtUtc
        WHERE  [Id]       = @Id
          AND  [IsActive] = 1;
        """;

    public const string InsertSeeded = """
        INSERT INTO [ops].[TempFile]
            ([FileName], [SizeBytes], [ExpiresAtUtc], [UtcTimeStamp], [UtcTimeStampLastUpdate])
        VALUES
            (@FileName, @SizeBytes, @ExpiresAtUtc, @NowUtc, @NowUtc);
        """;

    public const string CountAll = """
        SELECT COUNT(1) FROM [ops].[TempFile];
        """;
}
