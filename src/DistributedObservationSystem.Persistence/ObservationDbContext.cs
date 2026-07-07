using DistributedObservationSystem.Persistence.Entities;
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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ObservationDbContext).Assembly);
    }
}
