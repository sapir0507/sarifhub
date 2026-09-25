using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SarifHub.Infrastructure.Persistence.Json;

/// <summary>A string-to-string map stored as a <c>jsonb</c> object (tool-provided partial fingerprints).</summary>
internal static class StringMapJson
{
    public static ValueConverter<IReadOnlyDictionary<string, string>?, string> Converter { get; } = new(
        v => Serialize(v),
        v => Deserialize(v));

    public static ValueComparer<IReadOnlyDictionary<string, string>?> Comparer { get; } = new(
        (a, b) => Serialize(a) == Serialize(b),
        v => Serialize(v).GetHashCode(StringComparison.Ordinal),
        v => v);

    private static string Serialize(IReadOnlyDictionary<string, string>? map) =>
        JsonSerializer.Serialize(map, StoredJson.Options);

    private static Dictionary<string, string>? Deserialize(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json, StoredJson.Options);
}
