namespace DistributedObservationSystem.Contracts;

public sealed record ConnectConfirmRequest(string SensorId, string EncryptedSessionKey);
