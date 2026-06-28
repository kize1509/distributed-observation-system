using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "IngestionService", status = "healthy" }));

app.MapPost("/api/ingest/register", (
    SensorRegistrationRequest request,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Register");
    logger.LogInformation(
        "Received placeholder registration for sensor {SensorId} with quality {DataQuality}",
        request.SensorId,
        request.DataQuality);

    return Results.Accepted($"/api/ingest/sensors/{request.SensorId}");
});

app.MapPost("/api/ingest/readings", (
    SensorReadingRequest request,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Readings");
    logger.LogInformation(
        "Received placeholder reading from sensor {SensorId}: {Value} at {MeasuredAtUtc} with priority {AlarmPriority}",
        request.SensorId,
        request.Value,
        request.MeasuredAtUtc,
        request.AlarmPriority);

    return Results.Accepted();
});

app.MapPost("/api/ingest/sensors/{sensorId}/block", (
    string sensorId,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Block");
    logger.LogInformation("Received placeholder 30-second block request for sensor {SensorId}", sensorId);

    return Results.Accepted();
});

app.Run();
