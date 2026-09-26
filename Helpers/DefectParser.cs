using System.Text.RegularExpressions;

namespace ProductionMeeting.Helpers;

// Rework KPIs don't have a dedicated defect-breakdown table - instead, whoever enters the
// weekly value is expected to list defects in that row's Remarks as "<name> <count>",
// separated by semicolons, e.g. "DDS 150 RPT; K frame/cradle bolt half torque 63 RPT".
// This parses that convention into (name, count) pairs for the Top 5 Defects chart.
public static partial class DefectParser
{
    [GeneratedRegex(@"^(?<name>.+?)[\s:\-]+(?<count>\d+(\.\d+)?)\s*(RPT|Nos\.?|units?)?$", RegexOptions.IgnoreCase)]
    private static partial Regex EntryPattern();

    public static List<(string Name, int Count)> Parse(string? remarks)
    {
        var result = new List<(string, int)>();
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return result;
        }

        var segments = remarks.Split([';', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var trimmed = segment.Trim().TrimEnd('.');
            if (trimmed.Length == 0)
            {
                continue;
            }

            var match = EntryPattern().Match(trimmed);
            if (!match.Success || !int.TryParse(match.Groups["count"].Value, out var count))
            {
                continue;
            }

            var name = match.Groups["name"].Value.Trim().TrimEnd('-', ':').Trim();
            if (name.Length > 0)
            {
                result.Add((name, count));
            }
        }

        return result;
    }
}
