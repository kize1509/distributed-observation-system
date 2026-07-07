using DistributedObservationSystem.Persistence;
using Microsoft.EntityFrameworkCore;
using ReportingService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "ReportingService", status = "healthy" }));

app.MapGet("/api/reports/readings", async (
    ObservationDbContext db,
    string? sensorId,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? take) =>
{
    var query = db.SensorReadings.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(sensorId))
        query = query.Where(r => r.SensorId == sensorId);
    if (from is not null)
        query = query.Where(r => r.MeasuredAtUtc >= from);
    if (to is not null)
        query = query.Where(r => r.MeasuredAtUtc < to);

    var items = await query
        .OrderByDescending(r => r.MeasuredAtUtc)
        .Take(ReportQuery.ClampTake(take))
        .Select(r => new SensorReadingReportItem(r.SensorId, r.Value, r.MeasuredAtUtc, r.AlarmPriority, r.IsConsensus))
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/api/reports/alarms", async (
    ObservationDbContext db,
    string? sensorId,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? take) =>
{
    var query = db.AlarmEvents.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(sensorId))
        query = query.Where(a => a.SensorId == sensorId);
    if (from is not null)
        query = query.Where(a => a.MeasuredAtUtc >= from);
    if (to is not null)
        query = query.Where(a => a.MeasuredAtUtc < to);

    var items = await query
        .OrderByDescending(a => a.MeasuredAtUtc)
        .Take(ReportQuery.ClampTake(take))
        .Select(a => new AlarmEventReportItem(a.SensorId, a.Value, a.Priority, a.MeasuredAtUtc))
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/api/reports/consensus", async (
    ObservationDbContext db,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? take) =>
{
    var query = db.ConsensusReadings.AsNoTracking().AsQueryable();

    if (from is not null)
        query = query.Where(c => c.WindowStartUtc >= from);
    if (to is not null)
        query = query.Where(c => c.WindowStartUtc < to);

    var items = await query
        .OrderByDescending(c => c.WindowStartUtc)
        .Take(ReportQuery.ClampTake(take))
        .Select(c => new ConsensusReportItem(c.Value, c.WindowStartUtc, c.WindowEndUtc))
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/api/reports/sensors", async (ObservationDbContext db) =>
{
    var items = await db.Sensors
        .AsNoTracking()
        .OrderBy(s => s.Id)
        .Select(s => new SensorStatusReportItem(
            s.Id,
            s.DataQuality.ToString(),
            s.IsActive,
            s.LastMessageAtUtc,
            s.BlockedUntilUtc))
        .ToListAsync();

    return Results.Ok(items);
});

app.Run();
