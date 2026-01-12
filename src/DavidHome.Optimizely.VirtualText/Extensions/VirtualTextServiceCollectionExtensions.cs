using DavidHome.Optimizely.VirtualText;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using DavidHome.Optimizely.VirtualText.Routing;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextServiceCollectionExtensions
{
    public static IVirtualTextBuilder AddDavidHomeVirtualText(this IServiceCollection services)
    {
        services
            .AddSingleton(typeof(VirtualTextPartialRouter<>))
            .AddSingleton(typeof(IVirtualTextPartialRouterWrapper<>), typeof(VirtualTextPartialRouterWrapper<>));

        return new VirtualTextBuilder { Services = services };
    }
}