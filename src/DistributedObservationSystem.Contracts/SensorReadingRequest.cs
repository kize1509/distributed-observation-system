namespace DistributedObservationSystem.Contracts;

public sealed record SensorReadingRequest(
    string SensorId,
    decimal Value,
    DateTimeOffset MeasuredAtUtc,
    int AlarmPriority);
