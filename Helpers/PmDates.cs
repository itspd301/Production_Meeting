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

    // A week always runs Monday through Sunday (ISO 8601). Snaps to that week's Monday
    // first rather than trusting the caller already passed one, so the label is always
    // the full 7-day range regardless of which day of the week is passed in.
    public static string WeekLabel(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        var monday = ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);
        var sunday = monday.AddDays(6);
        return $"Week {week}, {year} ({monday:dd-MMM} - {sunday:dd-MMM})";
    }
}
