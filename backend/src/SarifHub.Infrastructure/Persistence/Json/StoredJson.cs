using System.Text.Json;
using System.Text.Json.Serialization;

namespace SarifHub.Infrastructure.Persistence.Json;

/// <summary>Serializer settings for JSON stored in <c>jsonb</c> columns: camelCase, enums as names.</summary>
internal static class StoredJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
