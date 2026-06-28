namespace DistributedObservationSystem.Contracts;

public sealed record SensorRegistrationRequest(
    string SensorId,
    decimal MinimumTemperature,
    decimal MaximumTemperature,
    DataQuality DataQuality);
