using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Persistence;
using DistributedObservationSystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConsensusService;

internal sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<ConsensusOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly ConsensusOptions _options = options.Value;
    private readonly Dictionary<string, int> _consecutiveRejections = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow;
        var nextMinute = TruncateToMinute(now).AddMinutes(1);
        var initialDelay = nextMinute - now;
        if (initialDelay > TimeSpan.Zero)
            await Task.Delay(initialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            now = DateTimeOffset.UtcNow;
            var windowEnd = TruncateToMinute(now);
            var windowStart = windowEnd.AddMinutes(-1);

            try
            {
                await ProcessWindowAsync(windowStart, windowEnd, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error processing consensus window [{Start}, {End}).",
                    windowStart, windowEnd);
            }

            var nextTick = TruncateToMinute(DateTimeOffset.UtcNow).AddMinutes(1);
            var remaining = nextTick - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, stoppingToken);
        }
    }

    private async Task ProcessWindowAsync(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ObservationDbContext>();

        var alreadyExists = await db.ConsensusReadings
            .AnyAsync(cr => cr.WindowStartUtc == windowStart, ct);
        if (alreadyExists)
            return;

        var goodSensorIds = await db.Sensors
            .Where(s => s.DataQuality == DataQuality.Good && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (goodSensorIds.Count == 0)
        {
            logger.LogWarning(
                "No active Good-quality sensors; skipping consensus for [{Start}, {End}).",
                windowStart, windowEnd);
            return;
        }

        var rawReadings = await db.SensorReadings
            .Where(sr =>
                sr.MeasuredAtUtc >= windowStart &&
                sr.MeasuredAtUtc < windowEnd &&
                goodSensorIds.Contains(sr.SensorId))
            .Select(sr => new { sr.Id, sr.SensorId, sr.Value })
            .ToListAsync(ct);

        if (rawReadings.Count < 2)
        {
            logger.LogWarning(
                "Only {Count} Good reading(s) in [{Start}, {End}); need ≥ 2. Skipping.",
                rawReadings.Count, windowStart, windowEnd);
            return;
        }

        var input = rawReadings.Select(r => (r.SensorId, r.Value)).ToList();

        var consensusValue = ConsensusCalculator.ConsensusValue(
            input,
            _options.MadMultiplier,
            out var rejectedSensorIds);

        if (consensusValue is null)
        {
            logger.LogWarning(
                "All {Count} readings in [{Start}, {End}) were outliers. Skipping.",
                rawReadings.Count, windowStart, windowEnd);
            return;
        }

        var rejectedSet = new HashSet<string>(rejectedSensorIds);
        var goodSensorSet = new HashSet<string>(goodSensorIds);

        var newlyBadSensorIds = new List<string>();

        foreach (var sensorId in rejectedSet)
        {
            _consecutiveRejections.TryGetValue(sensorId, out var prev);
            var streak = prev + 1;
            _consecutiveRejections[sensorId] = streak;

            if (streak >= _options.ConsecutiveRejectionsThreshold)
                newlyBadSensorIds.Add(sensorId);
        }

        foreach (var sensorId in goodSensorSet.Except(rejectedSet))
            _consecutiveRejections[sensorId] = 0;

        var consensusReadingIds = rawReadings
            .Where(r => !rejectedSet.Contains(r.SensorId))
            .Select(r => r.Id)
            .ToList();

        db.ConsensusReadings.Add(new ConsensusReading
        {
            Id = Guid.NewGuid(),
            Value = consensusValue.Value,
            WindowStartUtc = windowStart,
            WindowEndUtc = windowEnd,
            IsConsensus = true
        });

        if (consensusReadingIds.Count > 0)
        {
            await db.SensorReadings
                .Where(sr => consensusReadingIds.Contains(sr.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(sr => sr.IsConsensus, true), ct);
        }

        if (newlyBadSensorIds.Count > 0)
        {
            await db.Sensors
                .Where(s => newlyBadSensorIds.Contains(s.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(sr => sr.DataQuality, DataQuality.Bad), ct);

            foreach (var id in newlyBadSensorIds)
                logger.LogWarning(
                    "Sensor {SensorId} marked DataQuality=Bad after {Threshold} consecutive rejections.",
                    id, _options.ConsecutiveRejectionsThreshold);
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Consensus [{Start}, {End}): value={Value:F2}, kept={Kept}/{Total}, newlyBad={Bad}.",
            windowStart, windowEnd, consensusValue.Value,
            consensusReadingIds.Count, rawReadings.Count, newlyBadSensorIds.Count);
    }

    private static DateTimeOffset TruncateToMinute(DateTimeOffset dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, 0, dt.Offset);
}
