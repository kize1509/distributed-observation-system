using DistributedObservationSystem.Contracts;

var sensorId = Environment.GetEnvironmentVariable("SENSOR_ID") ?? $"sensor-{Environment.MachineName}";
var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:8080";

var registration = new SensorRegistrationRequest(
    sensorId,
    MinimumTemperature: 20,
    MaximumTemperature: 120,
    DataQuality.Good);

Console.WriteLine("[SensorClient] Starting placeholder sensor client.");
Console.WriteLine($"[SensorClient] SensorId={registration.SensorId}");
Console.WriteLine($"[SensorClient] ServerUrl={serverUrl}");
Console.WriteLine("[SensorClient] Phase 1 only logs startup. Measurement and secure messaging are implemented in Phase 2.");

while (true)
{
    Console.WriteLine($"[SensorClient] Placeholder heartbeat at {DateTimeOffset.UtcNow:O}");
    await Task.Delay(TimeSpan.FromSeconds(30));
}
