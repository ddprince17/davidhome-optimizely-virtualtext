using EPiServer.Applications;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Internal;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing.Pipeline.Internal;

namespace DavidHome.Optimizely.VirtualText.Core.Routing;

public class VirtualTextResolverPipelineStep : IUrlResolverPipelineStep
{
    private readonly IApplicationResolver _applicationResolver;

    public VirtualTextResolverPipelineStep(IApplicationResolver applicationResolver)
    {
        _applicationResolver = applicationResolver;
    }

    public RoutingState Resolve(UrlResolverContext context, UrlResolverOptions options)
    {
        var requestContent = context.Content;

        if (requestContent == null)
        {
            return RoutingState.Abort;
        }

        var requestStartPage = (_applicationResolver.GetByContent(requestContent.ContentLink, false) as IRoutableApplication)?.EntryPoint;

        // It would mean that this request fits something that would otherwise hit the home page, which is what we are looking for. 
        return Equals(requestStartPage, requestContent.ContentLink) ? RoutingState.Continue : RoutingState.Abort;
    }
}