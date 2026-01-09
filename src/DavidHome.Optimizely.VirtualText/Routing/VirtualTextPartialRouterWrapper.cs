using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Core;
using EPiServer.Core.Routing;

namespace DavidHome.Optimizely.VirtualText.Routing;

internal class VirtualTextPartialRouterWrapper<TContent> : IVirtualTextPartialRouterWrapper<TContent> where TContent : class, IContent
{
    public VirtualTextPartialRouterWrapper(VirtualTextPartialRouter<TContent> partialRouter)
    {
        PartialRouter = new PartialRouter<TContent, VirtualTextRoutedData>(partialRouter);
    }

    public PartialRouter PartialRouter { get; }
}