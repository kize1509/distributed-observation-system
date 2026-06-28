namespace ConsensusService;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "ConsensusService placeholder tick at {Time}. Real one-minute consensus is implemented in Phase 2.",
                DateTimeOffset.UtcNow);

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
