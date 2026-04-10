using DavidHome.Optimizely.VirtualText.Models;
using EPiServer;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DavidHome.Optimizely.VirtualText.Core;

internal class ApplicationEventsSubscriber : IEventSubscriber<ApplicationCreatedEvent>, IEventSubscriber<ApplicationUpdatedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PartialRouteHandler _partialRouteHandler;
    private readonly IContentLoader _contentLoader;
    private readonly ILogger<ApplicationEventsSubscriber> _logger;

    public ApplicationEventsSubscriber(IServiceProvider serviceProvider, PartialRouteHandler partialRouteHandler, IContentLoader contentLoader,
        ILogger<ApplicationEventsSubscriber> logger)
    {
        _serviceProvider = serviceProvider;
        _partialRouteHandler = partialRouteHandler;
        _contentLoader = contentLoader;
        _logger = logger;
    }

    public Task HandleAsync(ApplicationCreatedEvent eventData, EventContext context, CancellationToken cancellationToken = default)
    {
        return HandleApplicationChange(eventData, context, cancellationToken);
    }


    public Task HandleAsync(ApplicationUpdatedEvent eventData, EventContext context, CancellationToken cancellationToken = default)
    {
        return HandleApplicationChange(eventData, context, cancellationToken);
    }

    private Task HandleApplicationChange(ApplicationEvent eventData, EventContext context, CancellationToken cancellationToken)
    {
        var currentStartPage = (eventData.Application as IRoutableApplication)?.EntryPoint;

        if (eventData is ApplicationUpdatedEvent updateEventData)
        {
            var previousStartPage = (updateEventData.PreviousApplication as IRoutableApplication)?.EntryPoint;

            // If both references are the same, we don't need to update the routers.
            if (Equals(previousStartPage, currentStartPage))
            {
                return Task.CompletedTask;
            }
        }

        if (!_contentLoader.TryGet(currentStartPage, out IContent startPage))
        {
            return Task.CompletedTask;
        }

        var startPageType = startPage.GetOriginalType();
        var startPageRouter = _partialRouteHandler
            .GetIncomingRouters(startPageType)
            .FirstOrDefault(IsVirtualTextRouter(startPageType));

        // Router already exists, no need to register a new one.
        if (startPageRouter != null)
        {
            return Task.CompletedTask;
        }

        var genericPartialRouterWrapperType = typeof(IVirtualTextPartialRouterWrapper<>);
        var partialRouterWrapperType = genericPartialRouterWrapperType.MakeGenericType(startPageType);

        if (_serviceProvider.GetRequiredService(partialRouterWrapperType) is IVirtualTextPartialRouterWrapper routerWrapper)
        {
            _partialRouteHandler.RegisterPartialRouter(routerWrapper.PartialRouter);
        }
        else
        {
            _logger.LogError("Could not create partial router for the new/updated site.");
        }

        return Task.CompletedTask;
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
}