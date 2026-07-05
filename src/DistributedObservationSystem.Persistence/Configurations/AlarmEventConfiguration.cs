using DistributedObservationSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedObservationSystem.Persistence.Configurations;

public sealed class AlarmEventConfiguration : IEntityTypeConfiguration<AlarmEvent>
{
    public void Configure(EntityTypeBuilder<AlarmEvent> builder)
    {
        builder.HasKey(alarm => alarm.Id);
        builder.Property(alarm => alarm.Value).HasPrecision(10, 2);
        builder.HasIndex(alarm => new { alarm.SensorId, alarm.MeasuredAtUtc });
    }
}
