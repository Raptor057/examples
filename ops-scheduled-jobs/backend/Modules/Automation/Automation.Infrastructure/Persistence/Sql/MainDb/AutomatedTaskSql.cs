namespace Automation.Infrastructure.Persistence.Sql.MainDb;

/// <summary>
/// Todo el SQL de la configuracion de tareas, en un solo lugar y siempre parametrizado.
///
/// La sentencia que hay que leer entera es <see cref="ClaimDue"/>. El resto es administracion.
/// </summary>
internal static class AutomatedTaskSql
{
    /// <summary>Las columnas que reconstruyen el estado. Se repiten en varias consultas, asi que viven aqui.</summary>
    private const string StateColumns = """
                   [Code], [IsEnabled], [PausedReason], [ScheduleKind], [IntervalMinutes],
                   [RunAtLocalTimes], [LastRunAtUtc], [NextRunAtUtc], [LastStatus],
                   [LastCutoffUtc], [ClaimedAtUtc], [ClaimedBy]
        """;

    /// <summary>
    /// EL RECLAMO ATOMICO. Selecciona y reclama en UNA sola sentencia.
    ///
    /// Por que una sola:
    ///
    /// Con dos replicas -o durante un despliegue con solapamiento- las dos despiertan a la vez.
    /// Si se lee primero y se actualiza despues, entre las dos operaciones hay una ventana en
    /// la que ambas leen la misma tarea como libre, y las dos la ejecutan. El resumen se
    /// recalcula dos veces, el envio se manda dos veces, y no hay ningun error en ningun log.
    ///
    /// Aqui el motor decide: el UPDATE toma los candados, el OUTPUT devuelve exactamente las
    /// filas que ESTA instancia gano, y la otra se lleva una lista vacia.
    ///
    /// Los tres hints, uno por uno:
    ///
    /// - **READPAST**: si otra instancia ya tiene bloqueada una fila, esta se la SALTA en vez de
    ///   esperarla. Sin el, la segunda replica se queda esperando para descubrir al final que no
    ///   habia nada para ella.
    /// - **UPDLOCK**: toma el candado de actualizacion al leer, no despues. Es lo que cierra la
    ///   ventana entre "lo veo libre" y "lo marco mio".
    /// - **ROWLOCK**: pide candado de FILA. Sin el, el motor puede escalar a pagina y llevarse
    ///   por delante tareas vecinas que no tenian por que bloquearse.
    ///
    /// Dos condiciones del WHERE que no son de relleno:
    ///
    /// - <c>Code IN @KnownCodes</c>: los codigos que el CODIGO conoce. Una fila con un codigo
    ///   que no este en el catalogo nunca se reclama, asi que nadie puede dar de alta una tarea
    ///   con un INSERT.
    /// - <c>ClaimedAtUtc &lt; @StaleClaimBeforeUtc</c>: un reclamo lo bastante viejo se
    ///   considera abandonado y se puede retomar. Sin esta condicion, una instancia que muere a
    ///   media corrida congela su tarea para siempre.
    ///
    /// El OUTPUT devuelve tambien el reclamo ANTERIOR (deleted.*): si traia valor, este es un
    /// reclamo vencido que se retomo, y hay una corrida abierta que cerrar.
    /// </summary>
    public const string ClaimDue = """
        UPDATE t
        SET    t.[ClaimedAtUtc]           = @NowUtc,
               t.[ClaimedBy]              = @ClaimedBy,
               t.[UtcTimeStampLastUpdate] = @NowUtc
        OUTPUT inserted.[Code], inserted.[IsEnabled], inserted.[PausedReason],
               inserted.[ScheduleKind], inserted.[IntervalMinutes], inserted.[RunAtLocalTimes],
               inserted.[LastRunAtUtc], inserted.[NextRunAtUtc], inserted.[LastStatus],
               inserted.[LastCutoffUtc], inserted.[ClaimedAtUtc], inserted.[ClaimedBy],
               deleted.[ClaimedAtUtc] AS [PreviousClaimedAtUtc],
               deleted.[ClaimedBy]    AS [PreviousClaimedBy]
        FROM   [ops].[AutomatedTask] AS t WITH (READPAST, UPDLOCK, ROWLOCK)
        WHERE  t.[IsActive]      = 1
          AND  t.[IsEnabled]     = 1
          AND  t.[NextRunAtUtc] <= @NowUtc
          AND  (t.[ClaimedAtUtc] IS NULL OR t.[ClaimedAtUtc] < @StaleClaimBeforeUtc)
          AND  t.[Code] IN @KnownCodes;
        """;

    /// <summary>
    /// El reclamo de "ejecutar ahora": la misma sentencia sin la condicion de HORARIO.
    ///
    /// Lo que NO se quita es <c>IsEnabled = 1</c>. Ahi esta el punto: el boton se salta el
    /// horario, que es lo que se le pide, y no se salta la pausa. Quitar tambien esa condicion
    /// convertiria pausar en una sugerencia.
    /// </summary>
    public const string ClaimSpecific = """
        UPDATE t
        SET    t.[ClaimedAtUtc]           = @NowUtc,
               t.[ClaimedBy]              = @ClaimedBy,
               t.[UtcTimeStampLastUpdate] = @NowUtc
        OUTPUT inserted.[Code], inserted.[IsEnabled], inserted.[PausedReason],
               inserted.[ScheduleKind], inserted.[IntervalMinutes], inserted.[RunAtLocalTimes],
               inserted.[LastRunAtUtc], inserted.[NextRunAtUtc], inserted.[LastStatus],
               inserted.[LastCutoffUtc], inserted.[ClaimedAtUtc], inserted.[ClaimedBy],
               deleted.[ClaimedAtUtc] AS [PreviousClaimedAtUtc],
               deleted.[ClaimedBy]    AS [PreviousClaimedBy]
        FROM   [ops].[AutomatedTask] AS t WITH (READPAST, UPDLOCK, ROWLOCK)
        WHERE  t.[IsActive]  = 1
          AND  t.[IsEnabled] = 1
          AND  t.[Code]      = @Code
          AND  (t.[ClaimedAtUtc] IS NULL OR t.[ClaimedAtUtc] < @StaleClaimBeforeUtc);
        """;

    /// <summary>
    /// Cierra la corrida: mueve el cursor, agenda la siguiente y SUELTA el reclamo, todo en la
    /// misma sentencia. Separarlo dejaria estados intermedios donde el cursor ya avanzo pero la
    /// tarea sigue reclamada.
    /// </summary>
    public const string CompleteRun = """
        UPDATE [ops].[AutomatedTask]
        SET    [LastRunAtUtc]           = @LastRunAtUtc,
               [NextRunAtUtc]           = @NextRunAtUtc,
               [LastCutoffUtc]          = @LastCutoffUtc,
               [LastStatus]             = @LastStatus,
               [ClaimedAtUtc]           = NULL,
               [ClaimedBy]              = NULL,
               [UtcTimeStampLastUpdate] = @NowUtc
        WHERE  [Code] = @Code;
        """;

    /// <summary>
    /// Suelta el reclamo y nada mas. Se llama SIEMPRE en el finally del despachador. Volver a
    /// llamarla despues de CompleteRun no hace dano: la fila ya esta liberada.
    /// </summary>
    public const string ReleaseClaim = """
        UPDATE [ops].[AutomatedTask]
        SET    [ClaimedAtUtc]           = NULL,
               [ClaimedBy]              = NULL,
               [UtcTimeStampLastUpdate] = @NowUtc
        WHERE  [Code]          = @Code
          AND  [ClaimedAtUtc] IS NOT NULL;
        """;

    public const string SelectKnown = $"""
        SELECT {StateColumns}
        FROM   [ops].[AutomatedTask]
        WHERE  [IsActive] = 1
          AND  [Code] IN @KnownCodes
        ORDER  BY [Code];
        """;

    public const string SelectByCode = $"""
        SELECT {StateColumns}
        FROM   [ops].[AutomatedTask]
        WHERE  [IsActive] = 1
          AND  [Code]     = @Code;
        """;

    /// <summary>
    /// Pausa o reanuda. NextRunAtUtc solo se toca si viene con valor (al reanudar): el
    /// COALESCE evita necesitar dos sentencias casi iguales.
    /// </summary>
    public const string SetEnabled = """
        UPDATE [ops].[AutomatedTask]
        SET    [IsEnabled]              = @IsEnabled,
               [PausedReason]           = @PausedReason,
               [NextRunAtUtc]           = COALESCE(@NextRunAtUtc, [NextRunAtUtc]),
               [UtcTimeStampLastUpdate] = @NowUtc
        WHERE  [IsActive] = 1
          AND  [Code]     = @Code;
        """;

    /// <summary>
    /// El seeder. El WHERE NOT EXISTS es lo que hace que este INSERT sea seguro de correr en
    /// CADA arranque: da de alta lo que falta y no toca lo que ya esta.
    ///
    /// Pisar la configuracion existente seria el bug clasico de este patron: cada despliegue
    /// reanudaria las tareas que alguien pauso a proposito, y devolveria el cursor a su valor
    /// inicial, que es peor todavia porque reprocesa dias enteros.
    /// </summary>
    public const string InsertMissing = """
        INSERT INTO [ops].[AutomatedTask]
            ([Code], [IsEnabled], [ScheduleKind], [IntervalMinutes], [RunAtLocalTimes],
             [NextRunAtUtc], [LastCutoffUtc], [UtcTimeStamp], [UtcTimeStampLastUpdate])
        SELECT @Code, 1, @ScheduleKind, @IntervalMinutes, @RunAtLocalTimes,
               @NextRunAtUtc, @LastCutoffUtc, @NowUtc, @NowUtc
        WHERE  NOT EXISTS (SELECT 1 FROM [ops].[AutomatedTask] WHERE [Code] = @Code);
        """;
}
