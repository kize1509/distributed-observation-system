using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Security;

var sensorId = Environment.GetEnvironmentVariable("SENSOR_ID") ?? $"sensor-{Environment.MachineName}";
var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:8080";
var minTemp = decimal.Parse(Environment.GetEnvironmentVariable("MIN_TEMP") ?? "20");
var maxTemp = decimal.Parse(Environment.GetEnvironmentVariable("MAX_TEMP") ?? "120");
var dataQuality = Enum.TryParse<DataQuality>(Environment.GetEnvironmentVariable("DATA_QUALITY"), ignoreCase: true, out var dq) ? dq : DataQuality.Good;
var thresholdP1 = decimal.TryParse(Environment.GetEnvironmentVariable("THRESHOLD_P1"), out var t1) ? t1 : (decimal?)null;
var thresholdP2 = decimal.TryParse(Environment.GetEnvironmentVariable("THRESHOLD_P2"), out var t2) ? t2 : (decimal?)null;
var thresholdP3 = decimal.TryParse(Environment.GetEnvironmentVariable("THRESHOLD_P3"), out var t3) ? t3 : (decimal?)null;

var connectRequest = new ConnectRequest(sensorId, minTemp, maxTemp, dataQuality);

using var http = new HttpClient { BaseAddress = new Uri(serverUrl) };
var aesService = new AesGcmService();
var rsaService = new RsaHandshakeService();

Console.WriteLine($"[SensorClient] Starting. SensorId={sensorId}, ServerUrl={serverUrl}, DataQuality={dataQuality}");

var sessionKey = await HandshakeWithRetry();
long messageId = 0;

while (true)
{
    try
    {
        var sentAtUtc = DateTimeOffset.UtcNow;
        var temp = GenerateTemperature();
        var priority = ComputePriority(temp);
        var reading = new SensorReadingRequest(sensorId, temp, sentAtUtc, priority);
        var envelope = BuildEnvelope(reading, ++messageId, sentAtUtc);

        var response = await http.PostAsJsonAsync("/api/ingest/readings", envelope);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("[SensorClient] Session expired, re-connecting...");
            sessionKey = await HandshakeWithRetry();
            messageId = 0;
        }
        else
        {
            Console.ForegroundColor = priority switch
            {
                1 => ConsoleColor.Yellow,
                2 => ConsoleColor.DarkYellow,
                3 => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
            Console.WriteLine($"[SensorClient] Reading #{messageId} | Temp={temp:F2} | Priority={priority} | Status: {(int)response.StatusCode}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SensorClient] Error: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(10));
}

async Task<byte[]> HandshakeWithRetry()
{
    while (true)
    {
        try
        {
            return await Handshake();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorClient] Handshake failed: {ex.Message}. Retrying in 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

async Task<byte[]> Handshake()
{
    var connectResp = await http.PostAsJsonAsync("/api/ingest/connect", connectRequest);
    connectResp.EnsureSuccessStatusCode();
    var connectResponse = await connectResp.Content.ReadFromJsonAsync<ConnectResponse>();

    var key = rsaService.GenerateSessionKey();
    var encryptedKey = rsaService.EncryptSessionKey(connectResponse!.PublicKeyPem, key);
    var confirmRequest = new ConnectConfirmRequest(sensorId, Convert.ToBase64String(encryptedKey));

    var confirmResp = await http.PostAsJsonAsync("/api/ingest/connect/confirm", confirmRequest);
    confirmResp.EnsureSuccessStatusCode();

    Console.WriteLine("[SensorClient] Session established.");
    return key;
}

SecureMessageEnvelope BuildEnvelope(SensorReadingRequest reading, long msgId, DateTimeOffset sentAt)
{
    var plaintext = JsonSerializer.SerializeToUtf8Bytes(reading);
    var aad = Encoding.UTF8.GetBytes($"{sensorId}|{msgId}|{sentAt.Ticks}");
    var encrypted = aesService.Encrypt(sessionKey, plaintext, aad);
    return new SecureMessageEnvelope(sensorId, msgId, sentAt, Convert.ToBase64String(encrypted));
}

decimal GenerateTemperature()
{
    var range = (double)(maxTemp - minTemp);
    return minTemp + (decimal)(Random.Shared.NextDouble() * range);
}

int ComputePriority(decimal temp)
{
    if (thresholdP3.HasValue && temp >= thresholdP3.Value) return 3;
    if (thresholdP2.HasValue && temp >= thresholdP2.Value) return 2;
    if (thresholdP1.HasValue && temp >= thresholdP1.Value) return 1;
    return 0;
}
