using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace OpsJobs.Shared.SqlServer;

/// <summary>Abre una conexion ya abierta a partir de un connection string.</summary>
public class SqlDbConnectionFactory(string connectionString)
{
    protected readonly string ConnectionString = connectionString;

    public async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        // SQL Server: SqlConnection. Es el UNICO punto del proyecto que cambia si el motor
        // cambia; todo lo demas (marcador tipado, wrapper, clases *Sql) es identico.
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}

/// <summary>
/// Factory tipado: resuelve el connection string por el nombre del marcador.
/// ConfigurationSqlDbConnectionFactory&lt;MainDb&gt; -&gt; ConnectionStrings:MainDb.
/// </summary>
public class ConfigurationSqlDbConnectionFactory<TMarker>(IConfiguration configuration)
    : SqlDbConnectionFactory(
        configuration.GetConnectionString(typeof(TMarker).Name)
        ?? throw new InvalidOperationException(
            $"No se encontro el connection string '{typeof(TMarker).Name}'."));
