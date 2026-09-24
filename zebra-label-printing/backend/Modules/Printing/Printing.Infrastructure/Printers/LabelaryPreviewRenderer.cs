using Microsoft.Extensions.Options;
using Printing.Domain.Abstractions;

namespace Printing.Infrastructure.Printers;

/// <summary>
/// Convierte ZPL en una imagen llamando a Labelary, que es un servicio PUBLICO de terceros.
///
/// ⚠️ IMPLICACION QUE HAY QUE DECIR EN VOZ ALTA: el ZPL se manda por internet a un servidor que no
/// controlamos, y ese ZPL lleva los datos de la etiqueta -numero de parte, cliente, cantidades,
/// numeros de serie-. Para una vista previa con datos inventados es comodo; con datos de
/// produccion es una fuga.
///
/// Por eso viene APAGADO por omision (<c>Preview:Enabled</c>) y por eso esta detras de un puerto:
/// el dia que haga falta de verdad, se escribe un renderizador local y no se toca nada mas.
/// Ver ADR-0009.
/// </summary>
public sealed class LabelaryPreviewRenderer(
    HttpClient httpClient,
    IOptions<PreviewOptions> options) : ILabelPreviewRenderer
{
    private readonly PreviewOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled;

    public async Task<LabelPreview> RenderAsync(string zpl, int dpi, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new PreviewUnavailableException(
                "La vista previa esta apagada. Se enciende con Preview:Enabled, y conviene leer antes " +
                "por que esta apagada: manda el contenido de la etiqueta a un servicio de terceros.");
        }

        // Labelary mide en puntos por MILIMETRO, no por pulgada: 203 dpi son 8 dpmm y 300 son 12.
        var dpmm = dpi switch
        {
            <= 203 => "8dpmm",
            <= 300 => "12dpmm",
            _ => "24dpmm",
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/printers/{dpmm}/labels/{_options.LabelSize}/0/";

        using var content = new StringContent(zpl);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/png"));

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // El cuerpo del error de Labelary dice que comando ZPL no le gusto, y eso es
                // justo lo que quien escribe la plantilla necesita leer.
                var detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new PreviewUnavailableException(
                    $"El servicio de vista previa rechazo la etiqueta ({(int)response.StatusCode}). {Trim(detail)}");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            return new LabelPreview(bytes, "image/png");
        }
        catch (HttpRequestException ex)
        {
            throw new PreviewUnavailableException($"No se pudo contactar al servicio de vista previa. {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PreviewUnavailableException("El servicio de vista previa tardo demasiado.", ex);
        }
    }

    private static string Trim(string value)
        => value.Length <= 200 ? value : value[..200] + "...";
}

public sealed class PreviewOptions
{
    public const string SectionName = "Preview";

    /// <summary>
    /// Apagado por omision A PROPOSITO: encenderlo manda el contenido de las etiquetas a un
    /// tercero. Es una decision de quien opera, no un valor por omision comodo.
    /// </summary>
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://api.labelary.com";

    /// <summary>Tamaño en pulgadas, ancho x alto. 4x6 es la etiqueta de caja tipica.</summary>
    public string LabelSize { get; set; } = "4x6";

    public int TimeoutSeconds { get; set; } = 10;
}
