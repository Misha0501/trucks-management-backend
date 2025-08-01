namespace TruckManagement.Helpers;

public static class GuidHelper
{
    public static Guid? TryParseGuidOrThrow(string? input, string paramName)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        if (!Guid.TryParse(input, out var parsed))
            throw new ArgumentException(input, paramName);

        return parsed;
    }

    public static List<Guid> ParseGuids(IEnumerable<string>? rawValues, string paramName)
    {
        if (rawValues is null) return new();

        var result = new List<Guid>();
        foreach (var raw in rawValues)
        {
            if (!Guid.TryParse(raw, out var guid))
                throw new ArgumentException(raw, paramName);
            result.Add(guid);
        }

        return result;
    }
}