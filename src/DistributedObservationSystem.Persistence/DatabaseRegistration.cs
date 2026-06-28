using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedObservationSystem.Persistence;

public static class DatabaseRegistration
{
    public static IServiceCollection AddObservationDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ObservationDatabase")
            ?? "Host=localhost;Port=5432;Database=observation;Username=observation;Password=observation";

        services.AddDbContext<ObservationDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
