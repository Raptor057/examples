using Printing.Domain.Rules;
using Xunit;

namespace Printing.Tests;

/// <summary>
/// Las reglas de plantilla son puras: no tocan base ni impresora, asi que se prueban sin montar
/// nada. Es justo lo que justifica tenerlas en el dominio y no dentro del handler.
/// </summary>
public sealed class TemplateRulesTests
{
    [Theory]
    [InlineData("  box_label ", "BOX_LABEL")]
    [InlineData("BoxLabel", "BOXLABEL")]
    [InlineData(null, "")]
    public void El_codigo_se_normaliza_a_mayusculas(string? input, string expected)
        => Assert.Equal(expected, TemplateRules.NormalizeCode(input));

    [Theory]
    [InlineData(203, true)]
    [InlineData(300, true)]
    [InlineData(600, true)]
    [InlineData(200, false)] // 200 es la costumbre de planta; el cabezal real da 203
    [InlineData(0, false)]
    public void Solo_se_aceptan_las_resoluciones_del_catalogo(int dpi, bool supported)
        => Assert.Equal(supported, TemplateRules.IsSupportedDpi(dpi));

    [Fact]
    public void Los_marcadores_salen_sin_repetir_y_en_orden()
    {
        const string body = "^XA^FD{{PART_NUMBER}}^FS^FD{{serial}}^FS^FD{{PART_NUMBER}}^FS^XZ";
        Assert.Equal(["PART_NUMBER", "SERIAL"], TemplateRules.Placeholders(body));
    }

    [Fact]
    public void Un_marcador_sin_valor_queda_vacio_y_no_imprime_su_nombre()
    {
        // Una caja con "{{SERIAL}}" impreso es PEOR que una con el hueco en blanco: parece un dato.
        var rendered = TemplateRules.Render("^XA^FD{{SERIAL}}^FS^XZ", new Dictionary<string, string?>());
        Assert.Equal("^XA^FD^FS^XZ", rendered);
    }

    [Fact]
    public void Los_valores_se_sustituyen_sin_importar_espacios_en_el_marcador()
    {
        var values = new Dictionary<string, string?> { ["SERIAL"] = "GT-001" };
        Assert.Equal("^XA^FDGT-001^FS^XZ", TemplateRules.Render("^XA^FD{{ SERIAL }}^FS^XZ", values));
    }

    [Theory]
    [InlineData("A^B", "A_5EB")]
    [InlineData("A~B", "A_7EB")]
    [InlineData("A\\B", "A_5CB")]
    public void Un_valor_no_puede_convertirse_en_comando_ZPL(string input, string expected)
    {
        // El circunflejo abre un comando y la tilde cambia el caracter de control: un valor que
        // los traiga dejaria de ser dato y pasaria a ser instruccion para la impresora.
        Assert.Equal(expected, TemplateRules.EscapeForZpl(input));
    }

    [Fact]
    public void Los_saltos_de_linea_de_un_valor_se_vuelven_espacio()
    {
        // Un salto dentro de un ^FD parte el campo y la etiqueta sale cortada.
        Assert.Equal("linea 1 linea 2", TemplateRules.EscapeForZpl("linea 1\nlinea 2"));
    }

    [Fact]
    public void Se_avisa_de_los_valores_que_faltan_sin_impedir_la_impresion()
    {
        const string body = "^XA^FD{{A}}{{B}}{{C}}^FS^XZ";
        var values = new Dictionary<string, string?> { ["A"] = "1" };

        Assert.Equal(["B", "C"], TemplateRules.MissingValues(body, values));
    }

    [Theory]
    [InlineData(null, "n", "^XA^XZ", 203, "El codigo de la plantilla es obligatorio.")]
    [InlineData("c", null, "^XA^XZ", 203, "El nombre de la plantilla es obligatorio.")]
    [InlineData("c", "n", null, 203, "El cuerpo ZPL es obligatorio.")]
    [InlineData("c", "n", "^XA^XZ", 150, "El dpi debe ser uno de: 203, 300, 600.")]
    [InlineData("c", "n", "FD hola", 203, "El cuerpo ZPL debe empezar con ^XA.")]
    [InlineData("c", "n", "^XA hola", 203, "El cuerpo ZPL debe terminar con ^XZ.")]
    public void La_validacion_dice_exactamente_que_falta(string? code, string? name, string? body, int dpi, string expected)
        => Assert.Equal(expected, TemplateRules.Validate(code, name, body, dpi));

    [Fact]
    public void Una_plantilla_bien_formada_pasa()
        => Assert.Null(TemplateRules.Validate("BOX_LABEL", "Caja", "^XA^FD{{A}}^FS^XZ", 203));
}
