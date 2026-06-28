namespace DistributedObservationSystem.Contracts;

public sealed record AlarmNotificationRequest(
    string SensorId,
    decimal Value,
    int Priority,
    DateTimeOffset MeasuredAtUtc);
