using EPiServer.Core;
using EPiServer.Core.Routing;

namespace DavidHome.Optimizely.VirtualText;

internal interface IVirtualTextPartialRouterWrapper<TContent> : IVirtualTextPartialRouterWrapper where TContent : class, IContent;

internal interface IVirtualTextPartialRouterWrapper
{
    PartialRouter PartialRouter { get; }
}