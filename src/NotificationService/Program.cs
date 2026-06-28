using DistributedObservationSystem.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { service = "NotificationService", status = "healthy" }));

app.MapPost("/api/notifications/alarms", (
    AlarmNotificationRequest request,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("NotificationService.Alarms");
    logger.LogInformation(
        "Received placeholder alarm notification for sensor {SensorId}: {Value} priority {Priority}",
        request.SensorId,
        request.Value,
        request.Priority);

    return Results.Accepted();
});

app.MapHub<AlarmHub>("/hubs/alarms");

app.Run();

public sealed class AlarmHub : Microsoft.AspNetCore.SignalR.Hub
{
}
