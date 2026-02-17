namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;

public interface IRobotsEnvironmentIndexingSettingsStore
{
    Task<RobotsEnvironmentIndexingSetting?> GetAsync(string environmentName, CancellationToken cancellationToken = default);
    IAsyncEnumerable<RobotsEnvironmentIndexingSetting> ListAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(RobotsEnvironmentIndexingSetting setting, CancellationToken cancellationToken = default);
}
