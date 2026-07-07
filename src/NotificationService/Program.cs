using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Persistence;
using DistributedObservationSystem.Persistence.Entities;
using Microsoft.AspNetCore.Http.Connections;
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

// Ingress is a plain HTTP reverse proxy and cannot forward a WebSocket upgrade,
// so the hub is restricted to Long Polling, which works over ordinary request/response.
app.MapHub<AlarmHub>("/hubs/alarms", options =>
{
    options.Transports = HttpTransportType.LongPolling;
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(15);
});

app.Run();
