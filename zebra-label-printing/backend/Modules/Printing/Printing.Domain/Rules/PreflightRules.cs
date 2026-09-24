using Printing.Domain.Entities;

namespace Printing.Domain.Rules;

/// <summary>
/// Preguntarle a la impresora si esta en condiciones ANTES de mandarle la etiqueta.
///
/// Sin esto, mandar a una impresora sin papel "funciona": los bytes entran al buffer, la API
/// contesta que si, y la etiqueta no existe. El operador se entera cuando busca el papel y no
/// esta. Preguntar antes convierte ese silencio en un mensaje que dice que hacer.
/// </summary>
public static class PreflightRules
{
    /// <summary>
    /// Por que NO se debe mandar, o null si se puede. El texto esta escrito para el operador, no
    /// para el log: dice que hacer, no que bandera esta encendida.
    /// </summary>
    public static string? BlockingReason(PrinterStatusSnapshot status)
    {
        // El orden importa: se reporta lo que hay que arreglar PRIMERO. Con el cabezal abierto y
        // sin papel, decir "sin papel" manda a alguien a cargar rollo en una impresora abierta.
        if (status.IsHeadOpen) return "El cabezal de la impresora esta abierto. Cierralo y vuelve a intentar.";
        if (status.IsPaperOut) return "La impresora no tiene etiquetas. Carga el rollo y vuelve a intentar.";
        if (status.IsRibbonOut) return "La impresora no tiene ribbon. Cambialo y vuelve a intentar.";
        if (status.IsHeadTooHot) return "El cabezal esta demasiado caliente. Espera a que enfrie.";
        if (status.IsPaused) return "La impresora esta en pausa. Quitale la pausa desde su panel.";

        // El buffer lleno NO bloquea: significa que esta ocupada imprimiendo, y eso se resuelve
        // solo. Bloquear aqui rechazaria etiquetas en medio de un lote, que es justo cuando mas
        // se necesitan.
        return null;
    }

    /// <summary>
    /// Si conviene reintentar mas tarde o es inutil. Una impresora sin papel se arregla sola en
    /// cuanto alguien carga el rollo, asi que el trabajo SE ENCOLA. Un destino que no es una
    /// impresora Zebra no se arregla esperando.
    /// </summary>
    public static bool IsTemporary(PrinterStatusSnapshot status)
        => status.IsHeadOpen || status.IsPaperOut || status.IsRibbonOut || status.IsHeadTooHot || status.IsPaused;
}
