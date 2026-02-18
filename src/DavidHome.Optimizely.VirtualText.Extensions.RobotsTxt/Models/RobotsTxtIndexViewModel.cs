using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Models;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;

public class RobotsTxtIndexViewModel
{
    public string CurrentEnvironment { get; init; } = string.Empty;
    public IReadOnlyList<RobotsEnvironmentPolicyViewModel> Environments { get; init; } = [];
    public bool CanEdit { get; init; }
    public bool DisableRobotsTxtManipulator { get; init; }
    public string DefaultManipulatorContent { get; init; } = string.Empty;
}
