namespace TraceQ.Core.Utilities;

public static class TraceLinkParser
{
    private static readonly char[] Separators = [',', ';'];

    public static List<string> Parse(string? tracedTo)
    {
        if (string.IsNullOrWhiteSpace(tracedTo))
        {
            return new List<string>();
        }

        return tracedTo
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    public static string? Normalize(string? tracedTo)
    {
        var links = Parse(tracedTo);
        return links.Count == 0 ? null : string.Join(',', links);
    }
}
