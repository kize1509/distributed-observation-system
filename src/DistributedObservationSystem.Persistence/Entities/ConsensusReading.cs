namespace DistributedObservationSystem.Persistence.Entities;

public sealed class ConsensusReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public decimal Value { get; set; }

    public DateTimeOffset WindowStartUtc { get; set; }

    public DateTimeOffset WindowEndUtc { get; set; }

    public bool IsConsensus { get; set; } = true;
}
