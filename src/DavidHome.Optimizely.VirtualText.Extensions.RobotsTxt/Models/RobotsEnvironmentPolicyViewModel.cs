namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;

public class RobotsEnvironmentPolicyViewModel
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string? RobotsDirective { get; init; }
    public bool AllowIndexing { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsExplicit { get; init; }
}

public class RobotsTxtIndexViewModel
{
    public string CurrentEnvironment { get; init; } = string.Empty;
    public IReadOnlyList<RobotsEnvironmentPolicyViewModel> Environments { get; init; } = [];
    public bool CanEdit { get; init; }
}
