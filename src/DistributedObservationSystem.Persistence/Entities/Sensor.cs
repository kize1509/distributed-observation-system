using DistributedObservationSystem.Contracts;

namespace DistributedObservationSystem.Persistence.Entities;

public sealed class Sensor
{
    public string Id { get; set; } = string.Empty;

    public decimal MinimumTemperature { get; set; }

    public decimal MaximumTemperature { get; set; }

    public DataQuality DataQuality { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? LastMessageAtUtc { get; set; }

    public long LastMessageId { get; set; }

    public DateTimeOffset? BlockedUntilUtc { get; set; }
}
