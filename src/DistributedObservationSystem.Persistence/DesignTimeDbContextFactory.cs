using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DistributedObservationSystem.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ObservationDbContext>
{
    public ObservationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ObservationDatabase")
            ?? "Host=localhost;Port=5432;Database=observation;Username=observation;Password=observation";

        var options = new DbContextOptionsBuilder<ObservationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ObservationDbContext(options);
    }
}
