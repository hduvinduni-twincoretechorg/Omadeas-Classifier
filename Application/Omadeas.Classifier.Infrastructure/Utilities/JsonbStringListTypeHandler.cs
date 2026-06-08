using System.Data;
using System.Text.Json;
using Dapper;

namespace Omadeas.Classifier.Infrastructure.Utilities;

/// <summary>
/// Dapper type handler mapping a PostgreSQL <c>jsonb</c> array of strings (returned from
/// procedures as <c>text</c>) to/from a <see cref="List{String}"/>. Used for
/// <c>classifier.applies_to_node_types</c>. On write the service passes a pre-serialised JSON
/// string with a <c>::jsonb</c> cast, so <see cref="SetValue"/> is a safety net only.
/// </summary>
public class JsonbStringListTypeHandler : SqlMapper.TypeHandler<List<string>>
{
    public override List<string> Parse(object value)
    {
        if (value is null or DBNull)
            return null!;
        string json = value as string ?? value.ToString()!;
        if (string.IsNullOrWhiteSpace(json))
            return null!;
        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    public override void SetValue(IDbDataParameter parameter, List<string>? value)
    {
        parameter.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value);
    }
}
