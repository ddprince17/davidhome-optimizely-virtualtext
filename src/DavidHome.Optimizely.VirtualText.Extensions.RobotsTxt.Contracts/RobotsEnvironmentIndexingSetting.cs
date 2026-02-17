namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;

public class RobotsEnvironmentIndexingSetting
{
    public string EnvironmentName { get; init; } = string.Empty;
    public bool AllowIndexing { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
