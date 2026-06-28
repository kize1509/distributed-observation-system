using DistributedObservationSystem.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "ReportingService", status = "healthy" }));

app.MapGet("/api/reports/readings", (ILoggerFactory loggerFactory) =>
{
    loggerFactory.CreateLogger("ReportingService.Readings")
        .LogInformation("Received placeholder historical readings request.");

    return Results.Ok(Array.Empty<object>());
});

app.MapGet("/api/reports/alarms", (ILoggerFactory loggerFactory) =>
{
    loggerFactory.CreateLogger("ReportingService.Alarms")
        .LogInformation("Received placeholder alarms report request.");

    return Results.Ok(Array.Empty<object>());
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
