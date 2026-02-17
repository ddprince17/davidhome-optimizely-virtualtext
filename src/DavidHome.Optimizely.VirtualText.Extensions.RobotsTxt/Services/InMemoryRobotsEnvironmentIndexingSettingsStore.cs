using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;

internal class InMemoryRobotsEnvironmentIndexingSettingsStore : IRobotsEnvironmentIndexingSettingsStore
{
    private readonly Dictionary<string, RobotsEnvironmentIndexingSetting> _settings = new(StringComparer.OrdinalIgnoreCase);

    public Task<RobotsEnvironmentIndexingSetting?> GetAsync(string environmentName, CancellationToken cancellationToken = default)
    {
        _settings.TryGetValue(environmentName, out var setting);
        return Task.FromResult(setting);
    }

    public async IAsyncEnumerable<RobotsEnvironmentIndexingSetting> ListAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var setting in _settings.Values.OrderBy(setting => setting.EnvironmentName, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return setting;
            await Task.CompletedTask;
        }
    }

    public Task UpsertAsync(RobotsEnvironmentIndexingSetting setting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);
        _settings[setting.EnvironmentName] = setting;
        return Task.CompletedTask;
    }
}
