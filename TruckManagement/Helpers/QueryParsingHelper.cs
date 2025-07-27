using Microsoft.Extensions.Primitives;

namespace TruckManagement.Helpers
{
    public static class QueryParsingHelper
    {
        public static List<int> ParseIntQueryValues(StringValues rawValues)
        {
            return rawValues
                .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => int.TryParse(s, out var num) ? (int?)num : null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .ToList();
        }
    }
}
