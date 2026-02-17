using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    private const string ModuleName = "DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt";

    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddRobotsTxtExtension()
        {
            serviceBuilder.Services?
                .Configure<ProtectedModuleOptions>(options =>
                {
                    if (!options.Items.Any(item => item.Name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        options.Items.Add(new ModuleDetails { Name = ModuleName });
                    }
                })
                .AddTransient<IRobotsIndexingPolicyService, RobotsIndexingPolicyService>()
                .TryAddSingleton<IRobotsEnvironmentIndexingSettingsStore, InMemoryRobotsEnvironmentIndexingSettingsStore>();

            return serviceBuilder;
        }
    }
}
