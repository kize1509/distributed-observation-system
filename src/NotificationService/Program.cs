using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Persistence;
using DistributedObservationSystem.Persistence.Entities;
using Microsoft.AspNetCore.SignalR;
using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "NotificationService", status = "healthy" }));

app.MapPost("/api/notifications/alarms", async (
    AlarmNotificationRequest request,
    ObservationDbContext db,
    IHubContext<AlarmHub> hub,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("NotificationService.Alarms");

    db.AlarmEvents.Add(new AlarmEvent
    {
        Id = Guid.NewGuid(),
        SensorId = request.SensorId,
        Value = request.Value,
        Priority = request.Priority,
        MeasuredAtUtc = request.MeasuredAtUtc
    });
    await db.SaveChangesAsync();

    await hub.Clients.All.SendAsync("AlarmReceived", request);

    logger.LogInformation(
        "Recorded and broadcast alarm for sensor {SensorId}: {Value} priority {Priority}",
        request.SensorId,
        request.Value,
        request.Priority);

    return Results.Accepted();
});

app.MapHub<AlarmHub>("/hubs/alarms");

app.Run();
