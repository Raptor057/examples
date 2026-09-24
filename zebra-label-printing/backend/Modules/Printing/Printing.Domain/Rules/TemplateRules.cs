using System.Text;
using System.Text.RegularExpressions;

namespace Printing.Domain.Rules;

/// <summary>
/// Las reglas de una plantilla: que resoluciones se aceptan, como se normaliza un codigo y como
/// se rellenan los marcadores. Todo puro y sin dependencias, que es lo que lo hace probable sin
/// base de datos ni impresora.
/// </summary>
public static partial class TemplateRules
{
    /// <summary>
    /// Las resoluciones que este ejemplo acepta. 203 y no 200: el cabezal de las Zebra de 8
    /// puntos/mm da 203.2 dpi y el catalogo de Zebra lo llama 203. Mucha documentacion de planta
    /// dice "200" por costumbre; se acepta el numero real para que el dato no mienta.
    /// </summary>
    public static readonly int[] SupportedDpi = [203, 300, 600];

    public const int MaxCodeLength = 100;
    public const int MaxNameLength = 200;

    [GeneratedRegex(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex { get; }

    /// <summary>Mayusculas y sin espacios alrededor: el codigo es una llave, no un texto libre.</summary>
    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsSupportedDpi(int dpi) => Array.IndexOf(SupportedDpi, dpi) >= 0;

    /// <summary>Que marcadores declara la plantilla, sin repetidos y en orden de aparicion.</summary>
    public static IReadOnlyList<string> Placeholders(string? body)
    {
        if (string.IsNullOrEmpty(body)) return [];

        var seen = new List<string>();
        foreach (Match match in PlaceholderRegex.Matches(body))
        {
            var name = match.Groups[1].Value.ToUpperInvariant();
            if (!seen.Contains(name)) seen.Add(name);
        }
        return seen;
    }

    /// <summary>
    /// Los marcadores que la plantilla pide y nadie mando. Se devuelven para AVISAR, no para
    /// abortar: una etiqueta a la que le falta un dato opcional se sigue imprimiendo, y negarse
    /// dejaria a la linea parada por un campo que no se ve.
    /// </summary>
    public static IReadOnlyList<string> MissingValues(string? body, IReadOnlyDictionary<string, string?>? values)
    {
        var provided = values ?? new Dictionary<string, string?>();
        return [.. Placeholders(body).Where(p => !provided.ContainsKey(p))];
    }

    /// <summary>
    /// Sustituye cada <c>{{VARIABLE}}</c> por su valor. Un marcador sin valor queda VACIO, nunca
    /// con el texto del marcador: una etiqueta pegada a una caja con "{{SERIAL}}" impreso es peor
    /// que una con el hueco en blanco, porque parece un dato.
    /// </summary>
    public static string Render(string? body, IReadOnlyDictionary<string, string?>? values)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;

        var provided = values ?? new Dictionary<string, string?>();
        return PlaceholderRegex.Replace(body, match =>
        {
            var name = match.Groups[1].Value.ToUpperInvariant();
            provided.TryGetValue(name, out var value);
            return EscapeForZpl(value);
        });
    }

    /// <summary>
    /// En ZPL el acento circunflejo abre un comando y la tilde cambia el caracter de control: un
    /// valor que los traiga deja de ser dato y pasa a ser instruccion. Se neutralizan con el
    /// hexadecimal de ZPL, que es la unica forma de imprimirlos como texto.
    /// </summary>
    public static string EscapeForZpl(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '^': builder.Append("_5E"); break;
                case '~': builder.Append("_7E"); break;
                case '\\': builder.Append("_5C"); break;
                case '\r':
                case '\n': builder.Append(' '); break;
                default: builder.Append(character); break;
            }
        }
        return builder.ToString();
    }

    /// <summary>Por que la plantilla no es valida, o null si lo es.</summary>
    public static string? Validate(string? code, string? name, string? body, int dpi)
    {
        if (string.IsNullOrWhiteSpace(code)) return "El codigo de la plantilla es obligatorio.";
        if (NormalizeCode(code).Length > MaxCodeLength) return $"El codigo no puede pasar de {MaxCodeLength} caracteres.";
        if (string.IsNullOrWhiteSpace(name)) return "El nombre de la plantilla es obligatorio.";
        if (name.Trim().Length > MaxNameLength) return $"El nombre no puede pasar de {MaxNameLength} caracteres.";
        if (string.IsNullOrWhiteSpace(body)) return "El cuerpo ZPL es obligatorio.";
        if (!IsSupportedDpi(dpi)) return $"El dpi debe ser uno de: {string.Join(", ", SupportedDpi)}.";

        // Una etiqueta ZPL empieza en ^XA y termina en ^XZ. Sin eso la impresora se queda
        // esperando el cierre y la SIGUIENTE etiqueta sale pegada a esta o no sale.
        var trimmed = body.Trim();
        if (!trimmed.StartsWith("^XA", StringComparison.OrdinalIgnoreCase)) return "El cuerpo ZPL debe empezar con ^XA.";
        if (!trimmed.EndsWith("^XZ", StringComparison.OrdinalIgnoreCase)) return "El cuerpo ZPL debe terminar con ^XZ.";

        return null;
    }
}
