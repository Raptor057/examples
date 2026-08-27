namespace SaasAccessAudit.Shared.Postgres.Markers;

/// <summary>
/// Marcador tipado de base de datos. Su NOMBRE es la llave del connection string
/// (ConnectionStrings:MainDb). Una clase vacia por base: el repositorio declara en su firma
/// a que base habla, y eso se ve sin abrir el SQL.
/// </summary>
public sealed class MainDb;
