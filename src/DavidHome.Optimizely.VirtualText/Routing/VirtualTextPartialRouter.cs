using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using EPiServer.Web;

namespace DavidHome.Optimizely.VirtualText.Routing;

public class VirtualTextPartialRouter<TContent> : IPartialRouter<TContent, VirtualTextRoutedData> where TContent : class, IContent
{
    private readonly IVirtualFileLocationService _fileLocationService;
    private readonly ISiteDefinitionResolver _siteDefinitionResolver;
    private readonly ISiteDefinitionRepository _siteDefinitionRepository;

    public VirtualTextPartialRouter(IVirtualFileLocationService fileLocationService, ISiteDefinitionResolver siteDefinitionResolver,
        ISiteDefinitionRepository siteDefinitionRepository)
    {
        _fileLocationService = fileLocationService;
        _siteDefinitionResolver = siteDefinitionResolver;
        _siteDefinitionRepository = siteDefinitionRepository;
    }

    public object? RoutePartial(TContent content, UrlResolverContext segmentContext)
    {
        var contentSiteDefinition = _siteDefinitionResolver.GetByContent(content.ContentLink, false);
        var remainingSegments = segmentContext.RemainingSegments.Span.ToString(); // Using Span doesn't create a new string, it re-uses the same memory location.
        var siteId = contentSiteDefinition.Id.ToString("N");
        var fileLocations = _fileLocationService
            .QueryFileLocations(new VirtualFileLocationQuery
            {
                VirtualPath = remainingSegments
            })
            .Where(location => location.SiteId == siteId || string.IsNullOrEmpty(location.SiteId))
            .ToArrayAsync();
        var inMemoryLocations = AsyncHelper.RunSync(fileLocations.AsTask);
        var chosenFileLocation = inMemoryLocations.FirstOrDefault(location => location.SiteId == siteId) ??
                                 inMemoryLocations.FirstOrDefault(location => string.IsNullOrEmpty(location.SiteId));

        if (chosenFileLocation == null)
        {
            return null;
        }

        segmentContext.RemainingSegments = ReadOnlyMemory<char>.Empty; 

        return new VirtualTextRoutedData
        {
            SiteId = siteId,
            FileLocation = chosenFileLocation
        };
    }

    public PartialRouteData? GetPartialVirtualPath(VirtualTextRoutedData content, UrlGeneratorContext urlGeneratorContext)
    {
        if (!Guid.TryParse(content.SiteId, out var siteId) || Equals(siteId, Guid.Empty) || string.IsNullOrEmpty(content.FileLocation?.VirtualPath))
        {
            return null;
        }

        var siteDefinition = _siteDefinitionRepository.Get(siteId);

        if (siteDefinition != null)
        {
            return new PartialRouteData
            {
                BasePathRoot = siteDefinition.StartPage,
                PartialVirtualPath = content.FileLocation.VirtualPath
            };
        }

        return null;
    }
}
