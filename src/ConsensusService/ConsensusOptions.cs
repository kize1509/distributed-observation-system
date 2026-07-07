namespace ConsensusService;

internal sealed class ConsensusOptions
{
    public const string SectionName = "Consensus";

    public decimal MadMultiplier { get; init; } = 3.0m;
    public int ConsecutiveRejectionsThreshold { get; init; } = 3;
}
