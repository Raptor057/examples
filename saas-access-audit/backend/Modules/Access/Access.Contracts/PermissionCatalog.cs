namespace Access.Contracts;

/// <summary>Una entrada del catalogo. Es un POCO: viaja entre modulos sin arrastrar nada.</summary>
/// <param name="Code">Codigo canonico, en formato modulo:recurso:accion.</param>
/// <param name="Module">Modulo al que pertenece. Agrupa la pantalla de roles.</param>
/// <param name="Resource">Sobre que actua.</param>
/// <param name="Action">Que hace: view, manage, approve, export, o una accion propia.</param>
/// <param name="DisplayName">Como se llama para una persona.</param>
/// <param name="IsDestructive">
/// Marca informativa para la pantalla de roles: conceder esto tiene mas consecuencias que
/// conceder un view. No cambia como se evalua; cambia como se ve al concederlo.
/// </param>
public sealed record PermissionDefinition(
    string Code,
    string Module,
    string Resource,
    string Action,
    string DisplayName,
    bool IsDestructive = false);

/// <summary>
/// EL CATALOGO DE PERMISOS, DECLARADO EN CODIGO.
///
/// Esta lista es la unica fuente de verdad de que permisos EXISTEN en el producto. Al arrancar,
/// el sembrador la compara contra la tabla e inserta lo que falta; lo que esta en la tabla y ya
/// no esta aqui se desactiva. Consecuencias, que son justo el motivo de hacerlo asi:
///
///   - Agregar un permiso es UNA LINEA de codigo, no un script de base de datos que alguien
///     tiene que acordarse de correr en cada ambiente.
///   - Un permiso que no esta en esta lista NO EXISTE, aunque quede un renglon viejo en la
///     tabla: no se puede conceder desde la pantalla y ningun endpoint lo reconoce.
///
/// Y una aclaracion que ahorra medio dia de diagnostico: ESTAR EN EL CATALOGO NO ES TENERLO.
/// Sembrarlo solo lo hace ASIGNABLE. Si ningun rol lo tiene, la funcion se esconde para todos
/// y parece que no se desplego.
///
/// Vive en Access.Contracts -y no dentro del modulo- porque es del PRODUCTO, no del modulo que
/// administra roles: el modulo Audit tambien declara su politica con un codigo de esta lista, y
/// Contracts es la unica via legitima entre modulos.
/// </summary>
public static class PermissionCatalog
{
    public const string UsersView = "access:user:view";

    /// <summary>
    /// Accion destructiva: PERMISO PROPIO, no reusa un access:user:manage.
    /// Mover y destruir no son el mismo riesgo. Quien corrige el nombre de un usuario no
    /// necesariamente debe poder dejarlo fuera del sistema.
    /// </summary>
    public const string UsersDeactivate = "access:user:deactivate";

    public const string RolesView = "access:role:view";

    /// <summary>
    /// Cambiar quien puede hacer que afecta a OTRAS personas: por eso es permiso propio, lleva
    /// motivo obligatorio y deja bitacora, aunque no borre un solo dato.
    /// </summary>
    public const string RolesManage = "access:role:manage";

    /// <summary>
    /// La bitacora registra quien hizo que, y no todos deben verlo. Su consulta lleva permiso
    /// propio: sin el, la pantalla no aparece en el menu y el endpoint responde 403.
    /// </summary>
    public const string AuditLogView = "audit:log:view";

    /// <summary>La lista maestra. El orden es el que usa la pantalla de roles.</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(UsersView, "access", "user", "view", "Consultar usuarios"),
        new(UsersDeactivate, "access", "user", "deactivate", "Desactivar usuarios", IsDestructive: true),
        new(RolesView, "access", "role", "view", "Consultar roles y permisos"),
        new(RolesManage, "access", "role", "manage", "Conceder y revocar permisos", IsDestructive: true),
        new(AuditLogView, "audit", "log", "view", "Consultar la bitacora")
    ];

    private static readonly HashSet<string> Codes =
        new(All.Select(definition => definition.Code), StringComparer.Ordinal);

    /// <summary>
    /// Reconoce un codigo del catalogo. Lo usa la pantalla de roles antes de conceder y la
    /// prueba que recorre los endpoints: sin esto, un typo en un [HasPermission] se ve
    /// exactamente igual que "esta persona no tiene el permiso".
    /// </summary>
    public static bool Contains(string? code) => code is not null && Codes.Contains(code);

    public static PermissionDefinition? Find(string? code) =>
        code is null ? null : All.FirstOrDefault(definition => definition.Code == code);
}
