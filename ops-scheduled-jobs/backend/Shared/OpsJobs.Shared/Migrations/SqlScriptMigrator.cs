using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpsJobs.Shared.SqlServer;
using OpsJobs.Shared.SqlServer.Markers;

namespace OpsJobs.Shared.Migrations;

/// <summary>
/// El equivalente de "dotnet ef database update" cuando no hay EF Core.
///
/// Sin ORM, el esquema son scripts SQL numerados que alguien tiene que aplicar EN ORDEN y UNA
/// sola vez. Este migrador es ese alguien: lee el directorio, aplica lo que falta y anota lo
/// aplicado en un libro (dbo.SchemaMigration).
///
/// Dos guardias que valen mas que el resto del codigo:
///
/// 1. **Forward-only.** Si un script ya aplicado cambio de contenido, revienta. Editar una
///    migracion liberada es como reescribir un commit publicado: las bases que ya la aplicaron
///    quedan distintas de las que no, y nadie se entera hasta que algo falla lejos de la causa.
///    La correccion se hace con un script NUEVO.
///
/// 2. **Cada script, su transaccion.** Si el script 05 falla a la mitad, el 05 no queda a
///    medias y el 06 no se intenta. La base queda en el ultimo estado completo conocido.
/// </summary>
public sealed class SqlScriptMigrator(
    ConfigurationSqlDbConnection<MainDb> mainDb,
    ConfigurationSqlDbConnection<MasterDb> masterDb,
    IConfiguration configuration,
    ILogger<SqlScriptMigrator> logger)
{
    public async Task<int> ApplyAsync(CancellationToken cancellationToken = default)
    {
        var directory = ResolveScriptsDirectory();
        await EnsureDatabaseAsync(cancellationToken).ConfigureAwait(false);
        await mainDb.ExecuteAsync(SchemaMigrationSql.EnsureLedger, cancellationToken: cancellationToken).ConfigureAwait(false);

        var applied = (await mainDb.QueryAsync<AppliedScriptRow>(
                SchemaMigrationSql.SelectApplied, cancellationToken: cancellationToken).ConfigureAwait(false))
            .ToDictionary(row => row.ScriptName, row => row.Checksum, StringComparer.OrdinalIgnoreCase);

        // Orden por nombre de archivo: por eso los scripts llevan numero al frente. El orden
        // alfabetico y el cronologico tienen que ser el mismo, o el 10 se aplicaria antes del 2.
        var scripts = Directory
            .GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var newlyApplied = 0;
        foreach (var path in scripts)
        {
            var name = Path.GetFileName(path);
            var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var checksum = ComputeChecksum(text);

            if (applied.TryGetValue(name, out var storedChecksum))
            {
                if (!string.Equals(storedChecksum, checksum, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"El script '{name}' ya se aplico con otro contenido. Las migraciones son forward-only: " +
                        "corrige con un script nuevo en vez de editar el que ya se libero.");

                continue;
            }

            await ApplyScriptAsync(name, text, checksum, cancellationToken).ConfigureAwait(false);
            newlyApplied++;
        }

        logger.LogInformation(
            "Migraciones: {Applied} aplicadas ahora, {Total} en total en {Directory}.",
            newlyApplied, scripts.Count, directory);

        return newlyApplied;
    }

    private async Task ApplyScriptAsync(string name, string text, string checksum, CancellationToken cancellationToken)
    {
        var batches = SplitBatches(text);

        await mainDb.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            foreach (var batch in batches)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    batch, transaction: transaction, cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            // El libro se escribe DENTRO de la misma transaccion: si el script falla, tampoco
            // queda anotado como aplicado.
            await connection.ExecuteAsync(new CommandDefinition(
                SchemaMigrationSql.InsertApplied,
                new { ScriptName = name, Checksum = checksum, AppliedAtUtc = DateTime.UtcNow },
                transaction: transaction,
                cancellationToken: cancellationToken)).ConfigureAwait(false);

            return true;
        }, IsolationLevel.ReadCommitted, queryName: $"Migration.{name}", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Migracion aplicada: {ScriptName} ({BatchCount} lotes).", name, batches.Count);
    }

    /// <summary>
    /// La base todavia no existe la primera vez, asi que esta sentencia va contra master. Es el
    /// unico uso del marcador MasterDb.
    /// </summary>
    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        var mainConnectionString = configuration.GetConnectionString(nameof(MainDb))
            ?? throw new InvalidOperationException("No se encontro el connection string 'MainDb'.");
        var databaseName = new SqlConnectionStringBuilder(mainConnectionString).InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("El connection string 'MainDb' no declara una base de datos.");

        await masterDb.ExecuteAsync(
            SchemaMigrationSql.EnsureDatabase,
            new { DatabaseName = databaseName },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// GO no es SQL: es un separador de lotes que entiende la herramienta, no el motor. Si el
    /// script viaja entero por Dapper, el motor lo rechaza. Por eso se parte aqui.
    /// </summary>
    public static IReadOnlyList<string> SplitBatches(string script)
    {
        var batches = new List<string>();
        var current = new StringBuilder();

        foreach (var line in script.Split('\n'))
        {
            var trimmed = line.Trim().TrimEnd('\r');
            if (string.Equals(trimmed, "GO", StringComparison.OrdinalIgnoreCase))
            {
                AppendIfNotBlank(batches, current);
                current.Clear();
                continue;
            }

            current.AppendLine(line.TrimEnd('\r'));
        }

        AppendIfNotBlank(batches, current);
        return batches;
    }

    private static void AppendIfNotBlank(List<string> batches, StringBuilder builder)
    {
        var text = builder.ToString().Trim();
        if (text.Length > 0) batches.Add(text);
    }

    /// <summary>
    /// El checksum ignora saltos de linea para que un archivo guardado en Windows y el mismo
    /// archivo guardado en Linux no cuenten como scripts distintos.
    /// </summary>
    public static string ComputeChecksum(string script)
    {
        var normalized = script.Replace("\r\n", "\n", StringComparison.Ordinal);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    /// <summary>
    /// Los scripts se copian al directorio de salida (ver el csproj del Host), asi que se
    /// buscan junto al ejecutable. La configuracion puede apuntar a otro sitio.
    /// </summary>
    private string ResolveScriptsDirectory()
    {
        var configured = configuration["Migrations:Directory"];
        var directory = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "db", "migrations")
            : Path.GetFullPath(configured);

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"No se encontro el directorio de migraciones '{directory}'.");

        return directory;
    }

    private sealed record AppliedScriptRow(string ScriptName, string Checksum);
}
