namespace SarifHub.Infrastructure.Persistence;

/// <summary>
/// Builds CHECK constraint SQL for enums stored as text (data model §3.4). The allowed values are generated from the
/// C# enum, so the database constraint and the domain cannot drift apart.
/// </summary>
internal static class Checks
{
    /// <summary><c>column IN ('A', 'B', …)</c> for every name of <typeparamref name="TEnum"/> except <paramref name="except"/>.</summary>
    public static string In<TEnum>(string column, params TEnum[] except)
        where TEnum : struct, Enum
    {
        var names = Enum.GetValues<TEnum>()
            .Where(v => !except.Contains(v))
            .Select(v => $"'{v}'");
        return $"{column} IN ({string.Join(", ", names)})";
    }
}
