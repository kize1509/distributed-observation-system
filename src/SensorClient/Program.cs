using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DistributedObservationSystem.Contracts;
using DistributedObservationSystem.Security;

var sensorId = Environment.GetEnvironmentVariable("SENSOR_ID") ?? $"sensor-{Environment.MachineName}";
var serverUrl = Environment.GetEnvironmentVariable("SERVER_URL") ?? "http://localhost:8080";
var minTemp = decimal.Parse(Environment.GetEnvironmentVariable("MIN_TEMP") ?? "20");
var maxTemp = decimal.Parse(Environment.GetEnvironmentVariable("MAX_TEMP") ?? "120");

var connectRequest = new ConnectRequest(sensorId, minTemp, maxTemp, DataQuality.Good);

using var http = new HttpClient { BaseAddress = new Uri(serverUrl) };
var aesService = new AesGcmService();
var rsaService = new RsaHandshakeService();

Console.WriteLine($"[SensorClient] Starting. SensorId={sensorId}, ServerUrl={serverUrl}");

var sessionKey = await Handshake();
long messageId = 0;

while (true)
{
    try
    {
        var sentAtUtc = DateTimeOffset.UtcNow;
        var reading = new SensorReadingRequest(sensorId, GenerateTemperature(), sentAtUtc, 0);
        var envelope = BuildEnvelope(reading, ++messageId, sentAtUtc);

        var response = await http.PostAsJsonAsync("/api/ingest/readings", envelope);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("[SensorClient] Session expired, re-connecting...");
            sessionKey = await Handshake();
            messageId = 0;
        }
        else
        {
            Console.WriteLine($"[SensorClient] Reading #{messageId} sent. Status: {(int)response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SensorClient] Error: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(10));
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
