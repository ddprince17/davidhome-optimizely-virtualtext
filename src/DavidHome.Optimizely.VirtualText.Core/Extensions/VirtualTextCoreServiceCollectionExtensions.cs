using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Core;
using DavidHome.Optimizely.VirtualText.Core.Models;
using DavidHome.Optimizely.VirtualText.Core.Routing;
using DavidHome.Optimizely.VirtualText.Core.Services;
using DavidHome.Optimizely.VirtualText.Models;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

internal static class VirtualTextCoreServiceCollectionExtensions
{
    public static IVirtualTextBuilder AddDavidHomeVirtualTextCore(this IServiceCollection services)
    {
        services
            .AddHttpContextAccessor()
            .AddSingleton(typeof(VirtualTextPartialRouter<>))
            .AddSingleton(typeof(IVirtualTextPartialRouterWrapper<>), typeof(VirtualTextPartialRouterWrapper<>))
            .AddTransient<IVirtualFileBridgeService, VirtualFileBridgeService>();

        return new VirtualTextBuilder { Services = services };
    }
}
