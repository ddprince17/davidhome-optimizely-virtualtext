namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;

public class SaveRobotsEnvironmentSettingsRequest
{
    public string EnvironmentName { get; init; } = string.Empty;
    public string? RobotsDirective { get; init; }
}
