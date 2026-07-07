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

app.MapGet("/api/reports/consensus", (ILoggerFactory loggerFactory) =>
{
    loggerFactory.CreateLogger("ReportingService.Consensus")
        .LogInformation("Received placeholder consensus report request.");

    return Results.Ok(Array.Empty<object>());
});

app.MapGet("/api/reports/sensors", (ILoggerFactory loggerFactory) =>
{
    loggerFactory.CreateLogger("ReportingService.Sensors")
        .LogInformation("Received placeholder sensor status report request.");

    return Results.Ok(Array.Empty<object>());
});

app.Run();
