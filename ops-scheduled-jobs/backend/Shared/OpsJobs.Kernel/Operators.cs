namespace OpsJobs.Kernel;

/// <summary>
/// Los dos permisos que pide la skill background-jobs: uno para VER y otro para EJECUTAR y
/// PAUSAR. Estan aqui, en un proyecto sin dependencias, porque los usan por igual la capa web
/// (para las politicas) y la de aplicacion (para saber quien disparo una corrida).
/// </summary>
public static class OpsRoles
{
    /// <summary>Ve la lista de tareas y la bitacora. No puede tocar nada.</summary>
    public const string Reader = "ops.reader";

    /// <summary>Ademas de ver, ejecuta a mano y pausa o reanuda.</summary>
    public const string Administrator = "ops.admin";
}

/// <summary>Quien esta pidiendo. Sale SIEMPRE del token, nunca de un parametro del cliente.</summary>
public sealed record OperatorIdentity(string UserName, bool CanAdminister)
{
    /// <summary>Peticion sin identidad. Fail-closed: no ve nada y no puede nada.</summary>
    public static readonly OperatorIdentity Anonymous = new(string.Empty, false);
}

/// <summary>
/// Acceso al operador de la peticion en curso. La implementacion vive en la capa web porque es
/// la unica que sabe de HTTP; la capa de aplicacion solo conoce esta interfaz.
/// </summary>
public interface IOperatorContext
{
    OperatorIdentity Current { get; }
}
