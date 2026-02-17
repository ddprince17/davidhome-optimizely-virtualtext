using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Models;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Services;

public interface IRobotsIndexingPolicyService
{
    Task<string?> GetRobotsDirectiveForCurrentEnvironmentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RobotsEnvironmentPolicyViewModel>> ListVisibleEnvironmentsAsync(CancellationToken cancellationToken = default);
    Task SaveEnvironmentSettingAsync(string environmentName, string? robotsDirective, CancellationToken cancellationToken = default);
    Task ResetEnvironmentSettingAsync(string environmentName, CancellationToken cancellationToken = default);
}
