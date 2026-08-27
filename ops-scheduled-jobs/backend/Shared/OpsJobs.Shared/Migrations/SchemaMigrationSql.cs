namespace OpsJobs.Shared.Migrations;

/// <summary>
/// SQL del libro de migraciones. Igual que cualquier otra clase *Sql: parametrizado y en un
/// solo lugar. Estas tres sentencias no viven en un script numerado porque son las que hacen
/// que los scripts numerados se puedan aplicar.
/// </summary>
internal static class SchemaMigrationSql
{
    /// <summary>
    /// Crea la base si falta. El nombre de una base no puede viajar como parametro dentro de
    /// CREATE DATABASE -- un identificador no es un valor -- asi que se arma con QUOTENAME, que
    /// es la funcion que el motor ofrece justo para no concatenar identificadores a mano. El
    /// nombre sale del connection string de configuracion, nunca de una peticion.
    /// </summary>
    public const string EnsureDatabase = """
        IF DB_ID(@DatabaseName) IS NULL
        BEGIN
            DECLARE @createDatabase NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName);
            EXEC sys.sp_executesql @createDatabase;
        END
        """;

    /// <summary>Libro de lo aplicado. Sin esto, "idempotente" dependeria de cada script.</summary>
    public const string EnsureLedger = """
        IF OBJECT_ID(N'[dbo].[SchemaMigration]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[SchemaMigration]
            (
                [Id]            INT           IDENTITY(1,1) NOT NULL,
                [ScriptName]    NVARCHAR(200) NOT NULL,
                [Checksum]      CHAR(64)      NOT NULL,
                [AppliedAtUtc]  DATETIME2(3)  NOT NULL CONSTRAINT [DF_SchemaMigration_AppliedAtUtc] DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT [PK_SchemaMigration] PRIMARY KEY CLUSTERED ([Id] ASC),
                CONSTRAINT [UX_SchemaMigration_ScriptName] UNIQUE ([ScriptName])
            );
        END
        """;

    public const string SelectApplied = """
        SELECT [ScriptName], [Checksum]
        FROM   [dbo].[SchemaMigration]
        ORDER  BY [ScriptName];
        """;

    public const string InsertApplied = """
        INSERT INTO [dbo].[SchemaMigration] ([ScriptName], [Checksum], [AppliedAtUtc])
        VALUES (@ScriptName, @Checksum, @AppliedAtUtc);
        """;
}
