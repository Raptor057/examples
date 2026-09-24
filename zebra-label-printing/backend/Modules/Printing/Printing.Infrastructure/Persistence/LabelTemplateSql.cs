namespace Printing.Infrastructure.Persistence;

/// <summary>
/// Todo el SQL del modulo vive en clases como esta, nunca embebido en un repositorio ni en un
/// handler. Dos razones practicas: se puede leer el SQL del sistema entero abriendo una carpeta,
/// y una consulta que hay que probar a mano se copia y se pega sin desenredarla del codigo.
///
/// Los parametros SIEMPRE van parametrizados. Ni una concatenacion, ni una interpolacion con
/// datos de entrada: es la unica defensa real contra inyeccion, y no se negocia por comodidad.
/// </summary>
internal static class LabelTemplateSql
{
    private const string Columns = """
                Id            AS Id,
                Code          AS Code,
                Name          AS Name,
                Description   AS Description,
                Body          AS Body,
                Dpi           AS Dpi,
                Version       AS Version,
                IsActive      AS IsActive,
                CreatedAtUtc  AS CreatedAtUtc,
                UpdatedAtUtc  AS UpdatedAtUtc
        """;

    private const string VersionColumns = """
                Id            AS Id,
                Code          AS Code,
                Dpi           AS Dpi,
                Version       AS Version,
                Name          AS Name,
                Description   AS Description,
                Body          AS Body,
                CreatedAtUtc  AS CreatedAtUtc,
                ReplacedAtUtc AS ReplacedAtUtc
        """;

    /// <summary>
    /// Los filtros son opcionales y se resuelven con el patron "@x IS NULL OR columna = @x": una
    /// sola consulta en vez de armar el WHERE por concatenacion, que es justo donde se cuela la
    /// inyeccion.
    /// </summary>
    public static string List => $"""
        SELECT
        {Columns}
        FROM LabelTemplate
        WHERE (@Dpi IS NULL OR Dpi = @Dpi)
          AND (@IsActive IS NULL OR IsActive = @IsActive)
        ORDER BY Code, Dpi;
        """;

    public static string Find => $"""
        SELECT
        {Columns}
        FROM LabelTemplate
        WHERE Code = @Code AND Dpi = @Dpi;
        """;

    public static string Exists => """
        SELECT COUNT(1) FROM LabelTemplate WHERE Code = @Code AND Dpi = @Dpi;
        """;

    /// <summary>
    /// El alta escribe DOS filas: la plantilla vigente y su version 1 en el historial. Van en un
    /// solo comando para que SQLite las envuelva en la misma transaccion implicita: una plantilla
    /// sin su primera version en el historial seria una laguna desde el minuto cero.
    /// </summary>
    public static string Insert => $"""
        INSERT INTO LabelTemplate (Code, Name, Description, Body, Dpi, Version, IsActive, CreatedAtUtc)
        VALUES (@Code, @Name, @Description, @Body, @Dpi, 1, 1, @CreatedAtUtc);

        INSERT INTO LabelTemplateVersion (Code, Dpi, Version, Name, Description, Body, CreatedAtUtc)
        VALUES (@Code, @Dpi, 1, @Name, @Description, @Body, @CreatedAtUtc);

        SELECT
        {Columns}
        FROM LabelTemplate
        WHERE Code = @Code AND Dpi = @Dpi;
        """;

    /// <summary>
    /// La edicion ARCHIVA antes de pisar: marca la version vigente como reemplazada, guarda el
    /// cuerpo nuevo como version siguiente, y recien entonces actualiza la plantilla.
    ///
    /// El orden importa y las tres sentencias van juntas en un comando: si se actualizara primero,
    /// el cuerpo viejo ya no existiria para archivarlo, y el historial tendria un hueco justo en
    /// el cambio que alguien vendria a investigar.
    ///
    /// CreatedAtUtc no se toca: una edicion no puede reescribir cuando se creo algo.
    /// </summary>
    public static string Update => """
        UPDATE LabelTemplateVersion
        SET ReplacedAtUtc = @UpdatedAtUtc
        WHERE Code = @Code AND Dpi = @Dpi AND ReplacedAtUtc IS NULL;

        INSERT INTO LabelTemplateVersion (Code, Dpi, Version, Name, Description, Body, CreatedAtUtc)
        SELECT @Code, @Dpi, Version + 1, @Name, @Description, @Body, @UpdatedAtUtc
        FROM LabelTemplate
        WHERE Code = @Code AND Dpi = @Dpi;

        UPDATE LabelTemplate
        SET Name         = @Name,
            Description  = @Description,
            Body         = @Body,
            Version      = Version + 1,
            IsActive     = 1,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE Code = @Code AND Dpi = @Dpi;
        """;

    /// <summary>Baja logica. Nunca DELETE: hay etiquetas impresas que referencian esta plantilla.</summary>
    public static string Deactivate => """
        UPDATE LabelTemplate
        SET IsActive     = 0,
            UpdatedAtUtc = @UpdatedAtUtc
        WHERE Code = @Code AND Dpi = @Dpi AND IsActive = 1;
        """;

    /// <summary>El historial, de lo mas reciente a lo mas viejo. La vigente NO sale: ya esta en LabelTemplate.</summary>
    public static string History => $"""
        SELECT
        {VersionColumns}
        FROM LabelTemplateVersion
        WHERE Code = @Code AND Dpi = @Dpi AND ReplacedAtUtc IS NOT NULL
        ORDER BY Version DESC;
        """;

    public static string FindVersion => $"""
        SELECT
        {VersionColumns}
        FROM LabelTemplateVersion
        WHERE Code = @Code AND Dpi = @Dpi AND Version = @Version;
        """;
}
