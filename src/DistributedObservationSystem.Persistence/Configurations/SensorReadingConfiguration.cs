using DistributedObservationSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedObservationSystem.Persistence.Configurations;

public sealed class SensorReadingConfiguration : IEntityTypeConfiguration<SensorReading>
{
    public void Configure(EntityTypeBuilder<SensorReading> builder)
    {
        builder.HasKey(reading => reading.Id);
        builder.Property(reading => reading.Value).HasPrecision(10, 2);
        builder.HasIndex(reading => reading.MeasuredAtUtc);
        builder.HasIndex(reading => new { reading.SensorId, reading.MeasuredAtUtc });
    }
}
