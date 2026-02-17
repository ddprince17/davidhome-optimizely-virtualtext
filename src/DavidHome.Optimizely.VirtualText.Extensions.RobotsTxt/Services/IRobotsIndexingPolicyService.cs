using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;

public interface IRobotsIndexingPolicyService
{
    Task<bool> ShouldAllowIndexingCurrentEnvironmentAsync(CancellationToken cancellationToken = default);
    Task<string?> GetRobotsDirectiveForCurrentEnvironmentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RobotsEnvironmentPolicyViewModel>> ListVisibleEnvironmentsAsync(CancellationToken cancellationToken = default);
    Task SaveEnvironmentSettingAsync(string environmentName, bool allowIndexing, CancellationToken cancellationToken = default);
}
