using DistributedObservationSystem.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IngestionService;

internal sealed class InactivitySweeper(
    IServiceScopeFactory scopeFactory,
    ILogger<InactivitySweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during inactivity sweep.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservationDbContext>();

        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-10);

        var inactive = await db.Sensors
            .Where(s => s.IsActive && s.LastMessageAtUtc != null && s.LastMessageAtUtc < cutoff)
            .ToListAsync(ct);

        if (inactive.Count == 0) return;

        foreach (var sensor in inactive)
        {
            sensor.IsActive = false;
            logger.LogWarning("Sensor {SensorId} marked inactive (last seen: {LastMessage})",
                sensor.Id, sensor.LastMessageAtUtc);
        }

        await db.SaveChangesAsync(ct);
    }
}
