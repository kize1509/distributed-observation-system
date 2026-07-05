namespace DistributedObservationSystem.Persistence.Entities;

public sealed class SensorReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }

    public int AlarmPriority { get; set; }

    public bool IsConsensus { get; set; }
}
