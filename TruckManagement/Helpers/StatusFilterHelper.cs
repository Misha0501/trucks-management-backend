namespace TruckManagement.Helpers;

public static class StatusFilterHelper
{
    /// <summary>
    /// Parses the <c>statusIds</c> parameter into a list of <see cref="PartRideStatus"/>.
    /// Accepts  ▸ repeated keys  (?statusIds=0&amp;statusIds=2)  
    ///         ▸ comma-separated  (?statusIds=0,2)
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when a token is not an int or doesn’t map to <see cref="PartRideStatus"/>.
    /// </exception>
    public static List<PartRideStatus> ParseStatusIds(IEnumerable<string> rawValues)
    {
        var result = new List<PartRideStatus>();

        foreach (var raw in rawValues ?? Enumerable.Empty<string>())
        {
            foreach (var token in raw.Split(
                         ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(token, out var intVal) &&
                    Enum.IsDefined(typeof(PartRideStatus), intVal))
                {
                    result.Add((PartRideStatus)intVal);
                }
                else
                {
                    throw new ArgumentException(token);
                }
            }
        }

        return result;
    }
}