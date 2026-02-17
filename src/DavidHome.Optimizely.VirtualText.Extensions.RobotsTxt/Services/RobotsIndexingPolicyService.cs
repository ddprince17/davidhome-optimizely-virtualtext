using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;
using Microsoft.Extensions.Hosting;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;

public class RobotsIndexingPolicyService : IRobotsIndexingPolicyService
{
    private readonly IRobotsEnvironmentIndexingSettingsStore _settingsStore;
    private readonly IHostEnvironment _hostEnvironment;

    public RobotsIndexingPolicyService(IRobotsEnvironmentIndexingSettingsStore settingsStore, IHostEnvironment hostEnvironment)
    {
        _settingsStore = settingsStore;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<bool> ShouldAllowIndexingCurrentEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsStore.GetAsync(_hostEnvironment.EnvironmentName, cancellationToken);
        return setting?.AllowIndexing ?? IsProductionEnvironment(_hostEnvironment.EnvironmentName);
    }

    public async Task<string?> GetRobotsDirectiveForCurrentEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        return await ShouldAllowIndexingCurrentEnvironmentAsync(cancellationToken)
            ? null
            : RobotsTxtConstants.NoIndexDirective;
    }

    public async Task<IReadOnlyList<RobotsEnvironmentPolicyViewModel>> ListVisibleEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var explicitSettings = new Dictionary<string, RobotsEnvironmentIndexingSetting>(StringComparer.OrdinalIgnoreCase);

        await foreach (var setting in _settingsStore.ListAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(setting.EnvironmentName))
            {
                explicitSettings[setting.EnvironmentName.Trim()] = setting;
            }
        }

        var result = new List<RobotsEnvironmentPolicyViewModel>();
        var currentEnvironment = _hostEnvironment.EnvironmentName;

        if (!explicitSettings.TryGetValue(currentEnvironment, out var currentSetting))
        {
            result.Add(new RobotsEnvironmentPolicyViewModel
            {
                EnvironmentName = currentEnvironment,
                AllowIndexing = IsProductionEnvironment(currentEnvironment),
                IsCurrent = true,
                IsExplicit = false
            });
        }
        else
        {
            result.Add(new RobotsEnvironmentPolicyViewModel
            {
                EnvironmentName = currentSetting.EnvironmentName,
                AllowIndexing = currentSetting.AllowIndexing,
                IsCurrent = true,
                IsExplicit = true
            });

            explicitSettings.Remove(currentEnvironment);
        }

        result.AddRange(explicitSettings.Values
            .OrderBy(setting => setting.EnvironmentName, StringComparer.OrdinalIgnoreCase)
            .Select(setting => new RobotsEnvironmentPolicyViewModel
            {
                EnvironmentName = setting.EnvironmentName,
                AllowIndexing = setting.AllowIndexing,
                IsCurrent = false,
                IsExplicit = true
            }));

        return result;
    }

    public async Task SaveEnvironmentSettingAsync(string environmentName, bool allowIndexing, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            throw new ArgumentException("Environment name is required.", nameof(environmentName));
        }

        var normalizedEnvironmentName = environmentName.Trim();
        await _settingsStore.UpsertAsync(new RobotsEnvironmentIndexingSetting
        {
            EnvironmentName = normalizedEnvironmentName,
            AllowIndexing = allowIndexing,
            UpdatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static bool IsProductionEnvironment(string environmentName)
    {
        return string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);
    }
}
