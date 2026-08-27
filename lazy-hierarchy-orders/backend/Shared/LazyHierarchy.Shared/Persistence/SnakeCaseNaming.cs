using System.Text;

namespace LazyHierarchy.Shared.Persistence;

/// <summary>
/// Traduce identificadores de C# a snake_case. En PostgreSQL un identificador sin comillas se
/// pliega a minusculas, asi que escribir el esquema en snake_case es lo unico que permite que
/// el SQL a mano (Dapper) y el generado (EF) nombren las mismas columnas sin entrecomillar nada.
/// </summary>
public static class SnakeCaseNaming
{
    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (current == '_' || current == '-')
            {
                builder.Append('_');
                continue;
            }

            if (char.IsUpper(current) && i > 0)
            {
                var previous = name[i - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';
                if (previous != '_' && (!char.IsUpper(previous) || (char.IsUpper(previous) && char.IsLower(next))))
                    builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(current));
        }

        return builder.ToString();
    }
}
