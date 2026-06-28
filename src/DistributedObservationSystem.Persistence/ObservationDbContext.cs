using DistributedObservationSystem.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DistributedObservationSystem.Persistence;

public sealed class ObservationDbContext(DbContextOptions<ObservationDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors => Set<Sensor>();

    public DbSet<SensorReading> SensorReadings => Set<SensorReading>();

    public DbSet<AlarmEvent> AlarmEvents => Set<AlarmEvent>();

    public DbSet<ConsensusReading> ConsensusReadings => Set<ConsensusReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(sensor => sensor.Id);
            entity.Property(sensor => sensor.DataQuality).HasConversion<string>();
        });

        modelBuilder.Entity<SensorReading>(entity =>
        {
            entity.HasKey(reading => reading.Id);
            entity.Property(reading => reading.Value).HasPrecision(10, 2);
        });

        modelBuilder.Entity<AlarmEvent>(entity =>
        {
            entity.HasKey(alarm => alarm.Id);
            entity.Property(alarm => alarm.Value).HasPrecision(10, 2);
        });

        modelBuilder.Entity<ConsensusReading>(entity =>
        {
            entity.HasKey(consensus => consensus.Id);
            entity.Property(consensus => consensus.Value).HasPrecision(10, 2);
        });
    }
}

public sealed class Sensor
{
    public string Id { get; set; } = string.Empty;

    public decimal MinimumTemperature { get; set; }

    public decimal MaximumTemperature { get; set; }

    public DataQuality DataQuality { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? LastMessageAtUtc { get; set; }
}

public sealed class SensorReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }

    public int AlarmPriority { get; set; }

    public bool IsConsensus { get; set; }
}

public sealed class AlarmEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SensorId { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public int Priority { get; set; }

    public DateTimeOffset MeasuredAtUtc { get; set; }
}

public sealed class ConsensusReading
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public decimal Value { get; set; }

    public DateTimeOffset WindowStartUtc { get; set; }

    public DateTimeOffset WindowEndUtc { get; set; }

    public bool IsConsensus { get; set; } = true;
}
