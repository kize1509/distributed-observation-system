using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Persistence;
using DistributedObservationSystem.Persistence.Entities;
using DistributedObservationSystem.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddObservationDatabase(builder.Configuration);
builder.Services.AddSingleton<RsaHandshakeService>();
builder.Services.AddSingleton<AesGcmService>();
builder.Services.AddSingleton<HandshakeStore>();
builder.Services.AddHttpClient("notification", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Routes__NotificationService"] ?? "http://notification:8080");
});
builder.Services.AddHostedService<IngestionService.InactivitySweeper>();

var app = builder.Build();

var notificationClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient("notification");

app.MapGet("/healthz", () => Results.Ok(new { service = "IngestionService", status = "healthy" }));

app.MapPost("/api/ingest/sensors/{id}/block", async ( // For demonstrational purposes
    string id,
    ObservationDbContext db,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Block");

    var sensor = await db.Sensors.FindAsync(id);
    if (sensor is null)
        return Results.NotFound($"Sensor '{id}' not found.");

    sensor.BlockedUntilUtc = DateTimeOffset.UtcNow.AddSeconds(30);
    await db.SaveChangesAsync();

    logger.LogWarning("Sensor {SensorId} manually blocked for 30s", id);
    return Results.Ok(new { sensorId = id, blockedUntil = sensor.BlockedUntilUtc });
});

app.MapPost("/api/ingest/connect", (
    ConnectRequest request,
    HandshakeStore store,
    RsaHandshakeService rsa,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Connect");
    var (publicKeyPem, privateKey) = rsa.GenerateKeyPair();

    if (store.Pending.TryRemove(request.SensorId, out var stale))
        stale.PrivateKey.Dispose();

    store.Pending[request.SensorId] = (privateKey, request);
    logger.LogInformation("Handshake initiated for sensor {SensorId}", request.SensorId);
    return Results.Ok(new ConnectResponse(publicKeyPem));
});

app.MapPost("/api/ingest/connect/confirm", async (
    ConnectConfirmRequest request,
    HandshakeStore store,
    RsaHandshakeService rsa,
    ObservationDbContext db,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.ConnectConfirm");

    if (!store.Pending.TryRemove(request.SensorId, out var pending))
        return Results.BadRequest("No pending handshake for this sensor.");

    byte[] sessionKey;
    try
    {
        sessionKey = rsa.DecryptSessionKey(pending.PrivateKey, Convert.FromBase64String(request.EncryptedSessionKey));
    }
    catch (CryptographicException)
    {
        return Results.BadRequest("Session key decryption failed.");
    }
    finally
    {
        pending.PrivateKey.Dispose();
    }

    var sensor = await db.Sensors.FindAsync(request.SensorId);
    if (sensor is null)
    {
        db.Sensors.Add(new Sensor
        {
            Id = pending.Config.SensorId,
            MinimumTemperature = pending.Config.MinimumTemperature,
            MaximumTemperature = pending.Config.MaximumTemperature,
            DataQuality = pending.Config.DataQuality,
            IsActive = true,
            LastMessageId = 0
        });
    }
    else
    {
        sensor.MinimumTemperature = pending.Config.MinimumTemperature;
        sensor.MaximumTemperature = pending.Config.MaximumTemperature;
        sensor.DataQuality = pending.Config.DataQuality;
        sensor.IsActive = true;
        sensor.LastMessageId = 0;
    }
    await db.SaveChangesAsync();

    store.Sessions[request.SensorId] = sessionKey;
    logger.LogInformation("Session established for sensor {SensorId}", request.SensorId);
    return Results.Ok();
});

app.MapPost("/api/ingest/readings", async (
    SecureMessageEnvelope envelope,
    HandshakeStore store,
    AesGcmService aes,
    ObservationDbContext db,
    ILoggerFactory loggerFactory) =>
{
    var logger = loggerFactory.CreateLogger("IngestionService.Readings");

    if (!store.Sessions.TryGetValue(envelope.SensorId, out var sessionKey))
        return Results.Unauthorized();

    var sensor = await db.Sensors.FindAsync(envelope.SensorId);
    if (sensor is null)
        return Results.Unauthorized();

    if (sensor.BlockedUntilUtc.HasValue && sensor.BlockedUntilUtc.Value > DateTimeOffset.UtcNow)
        return Results.StatusCode(429);

    using var lease = store.AttemptAcquire(envelope.SensorId);
    if (!lease.IsAcquired)
    {
        sensor.BlockedUntilUtc = DateTimeOffset.UtcNow.AddSeconds(30);
        await db.SaveChangesAsync();
        logger.LogWarning("Rate limit exceeded for sensor {SensorId}, blocked for 30s", envelope.SensorId);
        return Results.StatusCode(429);
    }

    if (envelope.MessageId <= sensor.LastMessageId)
        return Results.BadRequest("Stale or replayed message.");

    if (Math.Abs((envelope.SentAtUtc - DateTimeOffset.UtcNow).TotalSeconds) > 30)
        return Results.BadRequest("Message timestamp outside acceptable window.");

    SensorReadingRequest reading;
    try
    {
        var aad = Encoding.UTF8.GetBytes($"{envelope.SensorId}|{envelope.MessageId}|{envelope.SentAtUtc.Ticks}");
        var plaintext = aes.Decrypt(sessionKey, Convert.FromBase64String(envelope.EncryptedPayload), aad);
        reading = JsonSerializer.Deserialize<SensorReadingRequest>(plaintext)!;
    }
    catch (CryptographicException)
    {
        return Results.BadRequest("Message authentication failed.");
    }

    db.SensorReadings.Add(new SensorReading
    {
        Id = Guid.NewGuid(),
        SensorId = envelope.SensorId,
        Value = reading.Value,
        MeasuredAtUtc = reading.MeasuredAtUtc,
        AlarmPriority = reading.AlarmPriority,
        IsConsensus = false
    });

    sensor.LastMessageId = envelope.MessageId;
    sensor.LastMessageAtUtc = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();

    logger.LogInformation(
        "Decrypted reading from sensor {SensorId}: {Value} at {MeasuredAtUtc}",
        envelope.SensorId, reading.Value, reading.MeasuredAtUtc);

    if (reading.AlarmPriority > 0)
    {
        Console.ForegroundColor = reading.AlarmPriority switch
        {
            1 => ConsoleColor.Yellow,
            2 => ConsoleColor.DarkYellow,
            3 => ConsoleColor.Red,
            _ => ConsoleColor.Gray
        };
        Console.WriteLine($"[ALARM P{reading.AlarmPriority}] Sensor={envelope.SensorId} Value={reading.Value:F2}");
        Console.ResetColor();

        _ = notificationClient.PostAsJsonAsync("/api/notifications/alarms",
            new AlarmNotificationRequest(envelope.SensorId, reading.Value, reading.AlarmPriority, reading.MeasuredAtUtc));
    }

    return Results.Accepted();
});

app.Run();
