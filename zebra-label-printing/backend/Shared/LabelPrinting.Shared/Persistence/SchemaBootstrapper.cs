using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LabelPrinting.Shared.Persistence;

/// <summary>
/// Crea el esquema al arrancar, corriendo en orden los .sql de la carpeta <c>db/</c>.
///
/// En un proyecto de verdad los scripts los aplica una persona y esto NO existe: el arranque de
/// una API no deberia poder alterar el esquema de produccion. Aqui esta a proposito para que el
/// ejemplo se levante con un comando y sin pasos manuales; el ADR-0005 lo dice con esas palabras.
/// </summary>
public sealed class SchemaBootstrapper(IConfiguration configuration, ILogger<SchemaBootstrapper> logger)
{
    public async Task ApplyAsync(string scriptsDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(scriptsDirectory))
        {
            logger.LogWarning("No existe la carpeta de scripts {Directory}; se arranca sin esquema.", scriptsDirectory);
            return;
        }

        var connectionString = configuration.GetConnectionString("LabelPrintingDb")
            ?? throw new InvalidOperationException("Falta la cadena ConnectionStrings:LabelPrintingDb.");

        // Los archivos se ordenan por nombre y por eso van numerados: 01, 02, 03. El orden no es
        // cosmetico -el 02 siembra en la tabla que crea el 01- y un orden alfabetico accidental
        // seria una bomba de tiempo.
        var scripts = Directory.GetFiles(scriptsDirectory, "*.sql").OrderBy(f => f, StringComparer.Ordinal).ToArray();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Script aplicado: {Script}", Path.GetFileName(script));
        }
    }
}
