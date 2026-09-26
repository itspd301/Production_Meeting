namespace ProductionMeeting.Helpers;

// Shared by both dashboards so "what counts as Green/Amber/Red" is defined in exactly
// one place: within 5% of target and on the wrong side counts as Amber, further off is Red.
public static class KpiStatusCalculator
{
    public static (string Status, decimal? Variance) Compute(bool lowerIsBetter, decimal? weekValue, decimal? target)
    {
        if (!weekValue.HasValue)
        {
            return ("Pending", null);
        }

        if (!target.HasValue)
        {
            return ("NoTarget", null);
        }

        var variance = weekValue - target;
        var good = lowerIsBetter ? weekValue <= target : weekValue >= target;

        if (good)
        {
            return ("Green", variance);
        }

        var tolerance = Math.Abs(target.Value) * 0.05m;
        var status = Math.Abs(variance!.Value) <= tolerance ? "Amber" : "Red";
        return (status, variance);
    }
}
