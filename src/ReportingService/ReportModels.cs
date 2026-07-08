namespace ReportingService;

internal static class ReportQuery
{
    private const int DefaultTake = 200;
    private const int MaxTake = 1000;

    public static int ClampTake(int? requested) =>
        Math.Clamp(requested ?? DefaultTake, 1, MaxTake);
}

public sealed record SensorReadingReportItem(
    string SensorId,
    decimal Value,
    DateTimeOffset MeasuredAtUtc,
    int AlarmPriority,
    bool IsConsensus);

public sealed record AlarmEventReportItem(
    string SensorId,
    decimal Value,
    int Priority,
    DateTimeOffset MeasuredAtUtc);

public sealed record ConsensusReportItem(
    decimal Value,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc);

public sealed record SensorStatusReportItem(
    string SensorId,
    string DataQuality,
    bool IsActive,
    DateTimeOffset? LastMessageAtUtc,
    DateTimeOffset? BlockedUntilUtc);
