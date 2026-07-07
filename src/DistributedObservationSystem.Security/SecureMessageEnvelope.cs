namespace DistributedObservationSystem.Security;

public sealed record SecureMessageEnvelope(
    string SensorId,
    long MessageId,
    DateTimeOffset SentAtUtc,
    string EncryptedPayload);
