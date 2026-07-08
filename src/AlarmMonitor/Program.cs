using DistributedObservationSystem.Contracts;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:8080";
var hubUrl = $"{serverUrl.TrimEnd('/')}/hubs/alarms";

Console.WriteLine($"[AlarmMonitor] Connecting to alarm hub at {hubUrl}");

// Ingress is a plain HTTP reverse proxy and cannot forward a WebSocket upgrade,
// so the client also pins Long Polling to skip the negotiate-then-upgrade attempt.
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl, options =>
    {
        options.Transports = HttpTransportType.LongPolling;
    })
    .WithAutomaticReconnect()
    .Build();

connection.On<AlarmNotificationRequest>("AlarmReceived", alarm =>
{
    Console.ForegroundColor = alarm.Priority switch
    {
        1 => ConsoleColor.Yellow,
        2 => ConsoleColor.DarkYellow,
        3 => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };
    Console.WriteLine(
        $"[ALARM P{alarm.Priority}] Sensor={alarm.SensorId} Value={alarm.Value:F2} MeasuredAt={alarm.MeasuredAtUtc:O}");
    Console.ResetColor();
});

connection.Reconnecting += ex =>
{
    Console.WriteLine($"[AlarmMonitor] Connection lost, reconnecting... {ex?.Message}");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine("[AlarmMonitor] Reconnected.");
    return Task.CompletedTask;
};

connection.Closed += async ex =>
{
    Console.WriteLine($"[AlarmMonitor] Connection closed: {ex?.Message}. Retrying in 5s...");
    await Task.Delay(TimeSpan.FromSeconds(5));
    await ConnectWithRetry();
};

await ConnectWithRetry();

Console.WriteLine("[AlarmMonitor] Listening for alarms. Press Ctrl+C to exit.");
await Task.Delay(Timeout.Infinite);

async Task ConnectWithRetry()
{
    while (true)
    {
        try
        {
            await connection.StartAsync();
            Console.WriteLine("[AlarmMonitor] Connected.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AlarmMonitor] Connect failed: {ex.Message}. Retrying in 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
