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
        var robotsDirective = await GetRobotsDirectiveForCurrentEnvironmentAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(robotsDirective);
    }

    public async Task<string?> GetRobotsDirectiveForCurrentEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _settingsStore.GetAsync(_hostEnvironment.EnvironmentName, cancellationToken);
        return ResolveDirective(setting, _hostEnvironment.EnvironmentName);
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
            var defaultDirective = ResolveDirective(null, currentEnvironment);
            result.Add(new RobotsEnvironmentPolicyViewModel
            {
                EnvironmentName = currentEnvironment,
                RobotsDirective = defaultDirective,
                AllowIndexing = string.IsNullOrWhiteSpace(defaultDirective),
                IsCurrent = true,
                IsExplicit = false
            });
        }
        else
        {
            var currentDirective = ResolveDirective(currentSetting, currentSetting.EnvironmentName);
            result.Add(new RobotsEnvironmentPolicyViewModel
            {
                EnvironmentName = currentSetting.EnvironmentName,
                RobotsDirective = currentDirective,
                AllowIndexing = string.IsNullOrWhiteSpace(currentDirective),
                IsCurrent = true,
                IsExplicit = true
            });

            explicitSettings.Remove(currentEnvironment);
        }

        result.AddRange(explicitSettings.Values
            .OrderBy(setting => setting.EnvironmentName, StringComparer.OrdinalIgnoreCase)
            .Select(setting =>
            {
                var directive = ResolveDirective(setting, setting.EnvironmentName);
                return new RobotsEnvironmentPolicyViewModel
                {
                    EnvironmentName = setting.EnvironmentName,
                    RobotsDirective = directive,
                    AllowIndexing = string.IsNullOrWhiteSpace(directive),
                    IsCurrent = false,
                    IsExplicit = true
                };
            }));

        return result;
    }

    public async Task SaveEnvironmentSettingAsync(string environmentName, string? robotsDirective, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            throw new ArgumentException("Environment name is required.", nameof(environmentName));
        }

        if (!RobotsDirectiveParser.TryNormalize(robotsDirective, out var normalizedDirective, out var error))
        {
            throw new ArgumentException(error ?? "Invalid robots directive.", nameof(robotsDirective));
        }

        var normalizedEnvironmentName = environmentName.Trim();
        await _settingsStore.UpsertAsync(new RobotsEnvironmentIndexingSetting
        {
            EnvironmentName = normalizedEnvironmentName,
            RobotsDirective = normalizedDirective,
            UpdatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static string? ResolveDirective(RobotsEnvironmentIndexingSetting? setting, string environmentName)
    {
        if (!string.IsNullOrWhiteSpace(setting?.RobotsDirective))
        {
            return setting.RobotsDirective;
        }

        if (setting is not null)
        {
            return null;
        }

        return IsProductionEnvironment(environmentName)
            ? null
            : RobotsTxtConstants.DefaultRestrictedDirective;
    }

    private static bool IsProductionEnvironment(string environmentName)
    {
        return string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);
    }
}
