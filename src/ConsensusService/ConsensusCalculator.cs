namespace ConsensusService;

internal static class ConsensusCalculator
{
    internal static decimal Median(IReadOnlyList<decimal> values)
    {
        var sorted = values.ToArray();
        Array.Sort(sorted);
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2m
            : sorted[mid];
    }

    internal static decimal Mad(IReadOnlyList<decimal> values, decimal median)
    {
        var deviations = values.Select(v => Math.Abs(v - median)).ToList();
        return Median(deviations);
    }

    internal static decimal? ConsensusValue(
        IReadOnlyList<(string SensorId, decimal Value)> readings,
        decimal madMultiplier,
        out IReadOnlyList<string> rejectedSensorIds)
    {
        var values = readings.Select(r => r.Value).ToList();
        var median = Median(values);
        var mad = Mad(values, median);
        var threshold = madMultiplier * mad;

        var kept = readings.Where(r => Math.Abs(r.Value - median) <= threshold).ToList();
        var rejected = readings.Where(r => Math.Abs(r.Value - median) > threshold).ToList();

        rejectedSensorIds = rejected.Select(r => r.SensorId).Distinct().ToList();

        if (kept.Count == 0)
            return null;

        return kept.Select(r => r.Value).Average();
    }
}
