using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Internal;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing.Pipeline.Internal;

namespace DavidHome.Optimizely.VirtualText.Core.Routing;

public class VirtualTextCheckPipelineStep : IUrlResolverPipelineStep
{
    public RoutingState Resolve(UrlResolverContext context, UrlResolverOptions options)
    {
        var partialRoutedObject = context.GetPartialRoutedObject();
        
        // If the routed data is not coming from the virtual text plugin, just abort this pipeline.
        return partialRoutedObject is VirtualTextRoutedData ? RoutingState.Done : RoutingState.Abort;
    }
}