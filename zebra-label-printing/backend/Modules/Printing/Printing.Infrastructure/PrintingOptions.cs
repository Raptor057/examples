namespace Printing.Infrastructure;

/// <summary>Seccion <c>Printing</c> de la configuracion.</summary>
public sealed class PrintingOptions
{
    public const string SectionName = "Printing";

    /// <summary>
    /// Que adaptador se usa: <c>zebra</c> (SDK de Link-OS, necesita impresoras de verdad) o
    /// <c>simulator</c> (escribe el ZPL en disco). Por omision el simulador, para que el ejemplo
    /// se levante y se pueda recorrer entero sin hardware.
    /// </summary>
    public string Driver { get; set; } = "simulator";

    /// <summary>Donde deja el simulador los .zpl que "imprime".</summary>
    public string SimulatorOutputPath { get; set; } = "printed-labels";

    /// <summary>
    /// Milisegundos de espera al abrir o leer de una impresora de red. El SDK trae los suyos y
    /// son generosos; en piso, una impresora que no contesta en 3 segundos esta apagada.
    /// </summary>
    public int NetworkTimeoutMs { get; set; } = 3000;

    /// <summary>Segundos que dura el barrido de red al descubrir impresoras.</summary>
    public int DiscoveryTimeoutSeconds { get; set; } = 5;
}
