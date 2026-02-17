using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    private const string ModuleName = "DavidHome.Optimizely.VirtualText";

    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddRobotsTxtExtension()
        {
            serviceBuilder.Services?
                .Configure<ProtectedModuleOptions>(options =>
                {
                    var assemblyName = typeof(VirtualTextBuilderExtensions).Assembly.GetName().Name ??
                                       throw new InvalidOperationException("Could not resolve RobotsTxt extension assembly name.");
                    var module = options.Items.FirstOrDefault(item => item.Name.Equals(VirtualTextServiceCollectionExtensions.ModuleName, StringComparison.OrdinalIgnoreCase));
                    if (module is null)
                    {
                        throw new InvalidOperationException(
                            $"{VirtualTextServiceCollectionExtensions.ModuleName} module is not registered. Call AddDavidHomeVirtualText() before AddRobotsTxtExtension().");
                    }

                    module.Assemblies.Add(assemblyName);
                })
                .AddTransient<IRobotsIndexingPolicyService, RobotsIndexingPolicyService>();

            return serviceBuilder;
        }
    }
}
