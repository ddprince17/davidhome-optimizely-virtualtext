using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Core;
using DavidHome.Optimizely.VirtualText.Core.Models;
using DavidHome.Optimizely.VirtualText.Core.Routing;
using EPiServer.Applications;
using EPiServer.DependencyInjection;

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
            .AddTransient<ApplicationEventsSubscriber>()
            .AddCmsEventSubscriber<ApplicationCreatedEvent>(provider => provider.GetRequiredService<ApplicationEventsSubscriber>())
            .AddCmsEventSubscriber<ApplicationUpdatedEvent>(provider => provider.GetRequiredService<ApplicationEventsSubscriber>());

        return new VirtualTextBuilder { Services = services };
    }
}
