using System.Globalization;

namespace ProductionMeeting.Helpers;

// Production meetings run weekly. A "week" is always represented internally by the
// Monday of that ISO week (stored in MeetingSession.MeetingDate), and in the UI by the
// HTML5 <input type="week"> value format "yyyy-Www".
public static class PmDates
{
    public static string ToWeekInputValue(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return $"{year}-W{week:D2}";
    }

    public static DateTime? FromWeekInputValue(string? weekValue)
    {
        if (string.IsNullOrWhiteSpace(weekValue))
        {
            return null;
        }

        var parts = weekValue.Split('-', 'W');
        if (parts.Length < 2 || !int.TryParse(parts[0], out var year))
        {
            return null;
        }

        var weekPart = parts[^1];
        if (!int.TryParse(weekPart, out var week))
        {
            return null;
        }

        return ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
    }

    // Snaps any date to the Monday of its ISO week. This is the single source of truth for
    // "what Monday does this date belong to" - every place that needs a week-aligned date
    // (session lookups, week labels, defaulting to "this week") must go through this rather
    // than trusting a date was already normalized, or re-deriving Monday its own way.
    public static DateTime GetMondayOfWeek(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
    }

    // A week always runs Monday through Sunday (ISO 8601).
    public static string WeekLabel(DateTime date)
    {
        var monday = GetMondayOfWeek(date);
        var sunday = monday.AddDays(6);
        return $"Week {ISOWeek.GetWeekOfYear(monday)}, {ISOWeek.GetYear(monday)} ({monday:dd-MMM} - {sunday:dd-MMM})";
    }
}
