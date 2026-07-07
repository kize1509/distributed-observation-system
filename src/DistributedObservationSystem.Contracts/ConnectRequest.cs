namespace DistributedObservationSystem.Contracts;

public sealed record ConnectRequest(
    string SensorId,
    decimal MinimumTemperature,
    decimal MaximumTemperature,
    DataQuality DataQuality);
