namespace DistributedObservationSystem.Persistence.Entities;

public sealed class AlarmEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public int Priority { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }
}
