using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SaasAccessAudit.Shared.Postgres;

/// <summary>Abre una conexion ya abierta a partir de un connection string.</summary>
public class SqlDbConnectionFactory(string connectionString)
{
    protected readonly string ConnectionString = connectionString;

    public async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}

/// <summary>
/// Factory tipado: resuelve el connection string por el nombre del marcador.
/// ConfigurationSqlDbConnectionFactory&lt;MainDb&gt; -> ConnectionStrings:MainDb.
/// </summary>
public class ConfigurationSqlDbConnectionFactory<TMarker>(IConfiguration configuration)
    : SqlDbConnectionFactory(
        configuration.GetConnectionString(typeof(TMarker).Name)
        ?? throw new InvalidOperationException(
            $"No se encontro el connection string '{typeof(TMarker).Name}'."));
