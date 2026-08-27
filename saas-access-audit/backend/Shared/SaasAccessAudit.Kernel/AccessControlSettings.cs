namespace SaasAccessAudit.Kernel;

/// <summary>
/// LA BANDERA DE ENFORCEMENT.
///
/// Decide si los permisos BLOQUEAN o solo se REGISTRAN. Existe para poder encender el control de
/// acceso sobre un sistema que ya esta en produccion sin tumbarle el trabajo a nadie: se despliega
/// apagada, se deja correr unos dias, se revisa en la bitacora quien habria chocado, se ajustan
/// los roles, y solo entonces se enciende.
///
/// Es un ESTADO DE TRANSICION, no un destino. Apagada en produccion es no tener control de acceso.
///
/// Se lee en cada decision, no una vez al arrancar: asi cambiar el valor en la configuracion surte
/// efecto sin reiniciar el servicio, que es lo que hace practico el "enciendelo y mira que pasa".
/// </summary>
public interface IAccessControlSettings
{
    bool EnforcePermissions { get; }
}
