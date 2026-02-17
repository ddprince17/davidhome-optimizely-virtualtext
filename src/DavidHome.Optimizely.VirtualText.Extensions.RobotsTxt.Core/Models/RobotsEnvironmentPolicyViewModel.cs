namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Models;

public class RobotsEnvironmentPolicyViewModel
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string? RobotsDirective { get; init; }
    public bool AllowIndexing { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsExplicit { get; init; }
}
