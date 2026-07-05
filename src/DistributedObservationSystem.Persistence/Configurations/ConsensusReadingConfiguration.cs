using DistributedObservationSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedObservationSystem.Persistence.Configurations;

public sealed class ConsensusReadingConfiguration : IEntityTypeConfiguration<ConsensusReading>
{
    public void Configure(EntityTypeBuilder<ConsensusReading> builder)
    {
        builder.HasKey(consensus => consensus.Id);
        builder.Property(consensus => consensus.Value).HasPrecision(10, 2);
        builder.HasIndex(consensus => consensus.WindowStartUtc);
    }
}
