using DistributedObservationSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedObservationSystem.Persistence.Configurations;

public sealed class SensorConfiguration : IEntityTypeConfiguration<Sensor>
{
    public void Configure(EntityTypeBuilder<Sensor> builder)
    {
        builder.HasKey(sensor => sensor.Id);
        builder.Property(sensor => sensor.DataQuality).HasConversion<string>();
        builder.Property(sensor => sensor.MinimumTemperature).HasPrecision(10, 2);
        builder.Property(sensor => sensor.MaximumTemperature).HasPrecision(10, 2);
    }
}
