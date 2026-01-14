using DavidHome.Optimizely.VirtualText;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using DavidHome.Optimizely.VirtualText.Routing;
using EPiServer.Shell.Modules;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextServiceCollectionExtensions
{
    private const string ModuleName = "DavidHome.Optimizely.VirtualText";

    public static IVirtualTextBuilder AddDavidHomeVirtualText(this IServiceCollection services)
    {
        services
            .AddSingleton(typeof(VirtualTextPartialRouter<>))
            .AddSingleton(typeof(IVirtualTextPartialRouterWrapper<>), typeof(VirtualTextPartialRouterWrapper<>))
            .Configure<ProtectedModuleOptions>(options =>
            {
                if (!options.Items.Any(item => item.Name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase)))
                {
                    options.Items.Add(new ModuleDetails { Name = ModuleName });
                }
            });

        return new VirtualTextBuilder { Services = services };
    }
}
