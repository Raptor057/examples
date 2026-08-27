namespace Automation.Infrastructure.Persistence;

/// <summary>
/// SQL Server devuelve DATETIME2 con <c>DateTimeKind.Unspecified</c>: la columna guarda la
/// fecha, no su zona. El valor es correcto -el esquema entero esta en UTC- pero .NET no lo sabe,
/// y ahi empiezan los problemas.
///
/// Sin marcar el Kind, una comparacion o una serializacion posterior puede tratar el valor como
/// hora LOCAL y desplazarlo. El sintoma no es una excepcion: son fechas corridas seis horas en
/// la pantalla, o una ventana que empieza donde no debe. Este es el unico sitio donde la capa de
/// datos declara lo que ya era cierto.
/// </summary>
internal static class UtcDates
{
    public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
}
