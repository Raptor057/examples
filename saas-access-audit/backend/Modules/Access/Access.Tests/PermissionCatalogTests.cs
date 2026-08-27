using System.Text.RegularExpressions;
using Access.Contracts;

namespace Access.Tests;

/// <summary>
/// El catalogo es la fuente de verdad de que permisos existen. Si se corrompe -codigos repetidos,
/// formatos distintos, una constante que no esta en la lista- el sistema no se rompe: se rompe la
/// CONFIANZA en el catalogo, y eso no da error, da diagnosticos imposibles.
/// </summary>
public sealed class PermissionCatalogTests
{
    private static readonly Regex CodeFormat = new(@"^[a-z]+:[a-z-]+:[a-z-]+$", RegexOptions.Compiled);

    [Fact]
    public void No_hay_codigos_repetidos()
    {
        var duplicates = PermissionCatalog.All
            .GroupBy(definition => definition.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.True(duplicates.Count == 0, $"Codigos repetidos en el catalogo: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Todo_codigo_sigue_el_formato_modulo_recurso_accion()
    {
        foreach (var definition in PermissionCatalog.All)
        {
            Assert.True(
                CodeFormat.IsMatch(definition.Code),
                $"El codigo '{definition.Code}' no sigue el formato modulo:recurso:accion.");
        }
    }

    [Fact]
    public void El_codigo_concuerda_con_sus_partes()
    {
        // Un codigo que dice una cosa y unas partes que dicen otra hace que la pantalla agrupe
        // los permisos por un modulo y el endpoint exija otro.
        foreach (var definition in PermissionCatalog.All)
        {
            Assert.Equal(
                $"{definition.Module}:{definition.Resource}:{definition.Action}",
                definition.Code);
        }
    }

    [Fact]
    public void Toda_constante_publica_esta_en_la_lista()
    {
        // Declarar la constante y olvidar agregarla a All es el error silencioso de este archivo:
        // el endpoint compila, exige un permiso que nadie sembro, y nadie puede concederlo.
        var constants = typeof(PermissionCatalog)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(constants);

        foreach (var code in constants)
        {
            Assert.True(
                PermissionCatalog.Contains(code),
                $"La constante '{code}' existe pero no esta en PermissionCatalog.All: ningun rol podria concederla.");
        }
    }

    [Fact]
    public void Una_accion_destructiva_no_reusa_el_manage_del_modulo()
    {
        // Mover y destruir no son el mismo riesgo. Quien puede corregir un dato no necesariamente
        // debe poder eliminarlo, asi que lo destructivo lleva su propia accion.
        foreach (var definition in PermissionCatalog.All.Where(item => item.IsDestructive))
        {
            Assert.True(
                definition.Action is not "view",
                $"El permiso destructivo '{definition.Code}' no puede ser una accion de solo consulta.");
        }

        Assert.Contains(PermissionCatalog.All, definition => definition.Action == "deactivate");
    }

    [Fact]
    public void Un_codigo_desconocido_no_esta_en_el_catalogo()
    {
        // Fail-closed: lo que no esta declarado, no existe.
        Assert.False(PermissionCatalog.Contains("access:user:borrar-todo"));
        Assert.False(PermissionCatalog.Contains(string.Empty));
        Assert.False(PermissionCatalog.Contains(null));
        Assert.Null(PermissionCatalog.Find("no-existe"));
    }
}
