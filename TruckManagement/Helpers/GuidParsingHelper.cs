namespace TruckManagement.Helpers;

public static class GuidParsingHelper
{
    public static List<Guid> ParseGuids(IEnumerable<string>? rawValues, string paramName)
    {
        if (rawValues is null) return new();

        var result = new List<Guid>();
        foreach (var raw in rawValues)
        {
            if (!Guid.TryParse(raw, out var guid))
                throw new ArgumentException($"Invalid GUID value '{raw}' for parameter '{paramName}'.");
            result.Add(guid);
        }
        return result;
    }
}