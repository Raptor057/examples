namespace LabelPrinting.Host.Access;

/// <summary>
/// Los permisos del sistema, declarados en CODIGO y no en una tabla.
///
/// Es deliberado: un permiso que no existe en el codigo no protege nada, asi que la lista
/// autoritativa es esta. Lo que va en base es a QUIEN se le concede, no cuales existen.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>Mandar etiquetas a imprimir. Es el permiso del piso.</summary>
    public const string Print = "printing:print";

    /// <summary>
    /// Crear, editar y dar de baja plantillas. Va SEPARADO de imprimir por una razon concreta:
    /// el cuerpo de una plantilla es ZPL con todos sus comandos, asi que quien puede editarla
    /// puede mandarle cualquier cosa a la impresora -reconfigurarla, borrar su memoria-. No es el
    /// mismo nivel de confianza que pulsar "imprimir".
    /// </summary>
    public const string ManageTemplates = "printing:templates:manage";

    /// <summary>Ver y cancelar trabajos de la cola.</summary>
    public const string ManageQueue = "printing:queue:manage";

    public static readonly string[] All = [Print, ManageTemplates, ManageQueue];
}
