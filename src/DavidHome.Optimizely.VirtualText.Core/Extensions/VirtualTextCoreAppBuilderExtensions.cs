using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Core;
using DavidHome.Optimizely.VirtualText.Core.Models;
using DavidHome.Optimizely.VirtualText.Core.Routing;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing.Pipeline.Internal;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

internal static class VirtualTextCoreAppBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        public IVirtualTextAppBuilder UseDavidHomeVirtualTextCore()
        {
            app.RegisterVirtualTextUrlResolver()
                .RegisterVirtualTextPartialRouters();

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
            var applicationRepository = app.ApplicationServices.GetRequiredService<IApplicationRepository>();
            var contentLoader = app.ApplicationServices.GetRequiredService<IContentLoader>();
            var genericPartialRouterWrapperType = typeof(IVirtualTextPartialRouterWrapper<>);

            foreach (var routableApplication in applicationRepository.List().OfType<IRoutableApplication>())
            {
                if (!contentLoader.TryGet(routableApplication.EntryPoint, out IContent startPage))
                {
                    continue;
                }

                var startPageType = startPage.GetOriginalType();
                var partialRouterWrapperType = genericPartialRouterWrapperType.MakeGenericType(startPageType);

                yield return partialRouterWrapperType;
            }
        }
    }

    private static PipelineDefinition GetVirtualTextPipelineDefinition()
    {
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
                    RoutingEntryPoint.StartPage
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