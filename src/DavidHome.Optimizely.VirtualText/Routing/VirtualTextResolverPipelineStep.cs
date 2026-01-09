using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Internal;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Core.Routing.Pipeline.Internal;
using EPiServer.Web;

namespace DavidHome.Optimizely.VirtualText.Routing;

public class VirtualTextResolverPipelineStep : IUrlResolverPipelineStep
{
    public RoutingState Resolve(UrlResolverContext context, UrlResolverOptions options)
    {
        var requestContent = context.Content;

        if (requestContent == null)
        {
            return RoutingState.Abort;
        }

        var requestStartPage = SiteDefinition.Current.StartPage;

        // It would mean that this request fits something that would otherwise hit the home page, which is what we are looking for. 
        return Equals(requestStartPage, requestContent.ContentLink) ? RoutingState.Continue : RoutingState.Abort;
    }
}