namespace OpsJobs.Shared.SqlServer.Markers;

/// <summary>
/// Marcador tipado de base de datos. Su NOMBRE es la llave del connection string
/// (ConnectionStrings:MainDb). Una clase vacia por base: el repositorio declara en su firma a
/// que base habla, y eso se ve sin abrir el SQL.
/// </summary>
public sealed class MainDb;

/// <summary>
/// La base del sistema. Existe por una sola razon: la imagen de SQL Server arranca sin la base
/// del proyecto, y CREATE DATABASE no se puede ejecutar desde una conexion a una base que
/// todavia no existe. Es tambien la demostracion barata del mecanismo multi-BD: el generic
/// elige a cual base apunta cada consulta, y no hay una sola cadena literal en el codigo.
/// </summary>
public sealed class MasterDb;
