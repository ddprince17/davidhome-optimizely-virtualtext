using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Core;
using DavidHome.Optimizely.VirtualText.Core.Models;
using DavidHome.Optimizely.VirtualText.Core.Routing;
using DavidHome.Optimizely.VirtualText.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing.Pipeline.Internal;
using EPiServer.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

internal static class VirtualTextCoreAppBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public IVirtualTextAppBuilder UseDavidHomeVirtualTextCore()
        {
            app.RegisterVirtualTextUrlResolver()
                .RegisterVirtualTextPartialRouters()
                .RegisterVirtualTextHostsListener();

            return new VirtualTextAppBuilder { Builder = app };
        }

        private IApplicationBuilder RegisterVirtualTextUrlResolver()
        {
            var resolverPipelineRegistry = app.ApplicationServices.GetRequiredService<UrlResolverPipelineRegistry>();
            var pipelineDefinition = GetVirtualTextPipelineDefinition();

            resolverPipelineRegistry.All.Add(pipelineDefinition);

            return app;
        }

        private IApplicationBuilder RegisterVirtualTextPartialRouters()
        {
            var routerTypes = app.GetVirtualTextRouterTypes().Distinct();
            var partialRouteHandler = app.ApplicationServices.GetRequiredService<PartialRouteHandler>();

            foreach (var routerType in routerTypes)
            {
                var routerWrapper = app.ApplicationServices.GetRequiredService(routerType) as IVirtualTextPartialRouterWrapper ??
                                    throw new ArgumentNullException(nameof(routerType), "Could not resolve partial router for Virtual Text Plugin.");

                partialRouteHandler.RegisterPartialRouter(routerWrapper.PartialRouter);
            }

            return app;
        }

        private IEnumerable<Type> GetVirtualTextRouterTypes()
        {
            var siteDefinitionRepository = app.ApplicationServices.GetRequiredService<ISiteDefinitionRepository>();
            var contentLoader = app.ApplicationServices.GetRequiredService<IContentLoader>();
            var genericPartialRouterWrapperType = typeof(IVirtualTextPartialRouterWrapper<>);

            foreach (var siteDefinition in siteDefinitionRepository.List())
            {
                if (!contentLoader.TryGet(siteDefinition.StartPage, out IContent startPage))
                {
                    continue;
                }

                var startPageType = startPage.GetOriginalType();
                var partialRouterWrapperType = genericPartialRouterWrapperType.MakeGenericType(startPageType);

                yield return partialRouterWrapperType;
            }
        }

        private IApplicationBuilder RegisterVirtualTextHostsListener()
        {
            var siteDefinitionEvents = app.ApplicationServices.GetRequiredService<ISiteDefinitionEvents>();

            siteDefinitionEvents.SiteCreated += SiteDefinitionChanged;
            siteDefinitionEvents.SiteUpdated += SiteDefinitionChanged;

            return app;

            IServiceProvider ServiceProvider() => app.ApplicationServices;

            void SiteDefinitionChanged(object? sender, SiteDefinitionEventArgs? e)
            {
                SiteDefinitionChangedInternal(ServiceProvider, e);
            }
        }
    }

    // Marker class for logging. It helps to identify the source of the log messages.
    private class VirtualTextAppBuilderExtensionsMarker;

    private static void SiteDefinitionChangedInternal(Func<IServiceProvider> serviceProvider, SiteDefinitionEventArgs? e)
    {
        var partialRouteHandler = serviceProvider().GetRequiredService<PartialRouteHandler>();
        var contentLoader = serviceProvider().GetRequiredService<IContentLoader>();
        var currentStartPage = e?.Site?.StartPage;

        if (e is SiteDefinitionUpdatedEventArgs updateEventArgs)
        {
            var previousStartPage = updateEventArgs.PreviousSite.StartPage;

            currentStartPage = updateEventArgs.Site?.StartPage;

            // If both references are the same, we don't need to update the routers.
            if (Equals(previousStartPage, currentStartPage))
            {
                return;
            }
        }

        if (!contentLoader.TryGet(currentStartPage, out IContent startPage))
        {
            return;
        }

        var startPageType = startPage.GetOriginalType();
        var startPageRouter = partialRouteHandler
            .GetIncomingRouters(startPageType)
            .FirstOrDefault(IsVirtualTextRouter(startPageType));

        if (startPageRouter != null)
        {
            return;
        }

        var genericPartialRouterWrapperType = typeof(IVirtualTextPartialRouterWrapper<>);
        var partialRouterWrapperType = genericPartialRouterWrapperType.MakeGenericType(startPageType);

        if (serviceProvider().GetRequiredService(partialRouterWrapperType) is IVirtualTextPartialRouterWrapper routerWrapper)
        {
            partialRouteHandler.RegisterPartialRouter(routerWrapper.PartialRouter);
        }
        else
        {
            var logger = serviceProvider().GetRequiredService<ILogger<VirtualTextAppBuilderExtensionsMarker>>();

            logger.LogError("[{className}] Could not create partial router for for the new/updated site.", nameof(VirtualTextCoreAppBuilderExtensions));
        }
    }

    private static Func<PartialRouter, bool> IsVirtualTextRouter(Type startPageType)
    {
        return router =>
        {
            var routerType = router.GetType();
            if (!routerType.IsGenericType)
            {
                return false;
            }

            var routerGenericTypes = routerType.GetGenericArguments();
            return routerGenericTypes.Length >= 2 && routerGenericTypes[0] == startPageType && routerGenericTypes[1] == typeof(VirtualTextRoutedData);
        };
    }

    private static PipelineDefinition GetVirtualTextPipelineDefinition()
    {
        var rootResolver = (Func<SiteDefinition, ContentReference>)(s => s.StartPage);
        var pipelineDefinition = new PipelineDefinition("VirtualTextRoute", RouteContextMode.Default)
        {
            new PipelineStepDefinition
            {
                Type = typeof(HostUrlResolverPipelineStep)
            },
            new PipelineStepDefinition
            {
                Type = typeof(LanguageUrlResolverPipelineStep),
                CustomArguments =
                [
                    false
                ]
            },
            new PipelineStepDefinition
            {
                Type = typeof(HierarchicalUrlResolverPipelineStep),
                CustomArguments =
                [
                    true,
                    rootResolver
                ]
            },
            new PipelineStepDefinition
            {
                Type = typeof(VirtualTextResolverPipelineStep)
            },
            new PipelineStepDefinition
            {
                Type = typeof(PartialUrlResolverPipelineStep)
            },
            new PipelineStepDefinition
            {
                Type = typeof(VirtualTextCheckPipelineStep)
            }
        };

        return pipelineDefinition;
    }
}