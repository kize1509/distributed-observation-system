using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using DistributedObservationSystem.Contracts;

internal sealed class HandshakeStore
{
    public ConcurrentDictionary<string, (RSA PrivateKey, ConnectRequest Config)> Pending { get; } = new();
    public ConcurrentDictionary<string, byte[]> Sessions { get; } = new();
    private ConcurrentDictionary<string, SlidingWindowRateLimiter> RateLimiters { get; } = new();

    public RateLimitLease AttemptAcquire(string sensorId)
    {
        var limiter = RateLimiters.GetOrAdd(sensorId, _ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 5,
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.NewestFirst,
            QueueLimit = 0
        }));
        return limiter.AttemptAcquire(1);
    }
}
