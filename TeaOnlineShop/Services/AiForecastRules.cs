namespace TeaOnlineShop.Services;

public static class AiForecastRules
{
    public const int DemandInputWindowDays = 60;

    public static readonly IReadOnlySet<int> DemandHorizons = new HashSet<int>
    {
        30,
        45,
        60
    };

    public static bool IsSupportedDemandHorizon(int horizonDays) =>
        DemandHorizons.Contains(horizonDays);

    public static int CalculateCoverageDays(DateTime? oldestObservationUtc, DateTime cutoffUtc, int requiredDays)
    {
        if (!oldestObservationUtc.HasValue || requiredDays <= 0)
            return 0;

        var inclusiveDays = (cutoffUtc.Date - oldestObservationUtc.Value.Date).Days + 1;
        return Math.Min(requiredDays, Math.Max(0, inclusiveDays));
    }
}

public static class ForecastMetrics
{
    public static double? MeanAbsolutePercentageError(
        IEnumerable<(double Predicted, double? Actual)> observations)
    {
        var errors = observations
            .Where(x => x.Actual.HasValue && x.Actual.Value != 0d)
            .Select(x => Math.Abs((x.Actual!.Value - x.Predicted) / x.Actual.Value) * 100d)
            .ToList();

        return errors.Count == 0 ? null : errors.Average();
    }
}
