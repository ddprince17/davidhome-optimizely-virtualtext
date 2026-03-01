using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Applications;
using EPiServer.Core;
using EPiServer.Core.Routing;
using EPiServer.Core.Routing.Pipeline;
using Microsoft.AspNetCore.Http;

namespace DavidHome.Optimizely.VirtualText.Core.Routing;

public class VirtualTextPartialRouter<TContent> : IPartialRouter<TContent, VirtualTextRoutedData> where TContent : class, IContent
{
    private const string AsteriskHost = "*";
    private readonly IVirtualFileLocationService _fileLocationService;
    private readonly IApplicationResolver _applicationResolver;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VirtualTextPartialRouter(IVirtualFileLocationService fileLocationService, IApplicationResolver applicationResolver,
        IApplicationRepository applicationRepository, IHttpContextAccessor httpContextAccessor)
    {
        _fileLocationService = fileLocationService;
        _applicationResolver = applicationResolver;
        _applicationRepository = applicationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public object? RoutePartial(TContent content, UrlResolverContext segmentContext)
    {
        var contentApplication = _applicationResolver.GetByContent(content.ContentLink, false);
        var remainingSegments = segmentContext.RemainingSegments.Span.ToString(); // Using Span doesn't create a new string, it re-uses the same memory location.
        var siteId = contentApplication?.Name;
        var fileLocations = _fileLocationService
            .QueryFileLocations(new VirtualFileLocationQuery { VirtualPaths = [remainingSegments] })
            .Where(location => location.SiteId == siteId || string.IsNullOrEmpty(location.SiteId))
            .ToArrayAsync();
        var inMemoryLocations = AsyncHelper.RunSync(fileLocations.AsTask);
        var chosenFileLocation = PickLocation(inMemoryLocations, siteId);

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

    private VirtualFileLocation? PickLocation(IReadOnlyCollection<VirtualFileLocation> locations, string? siteId)
    {
        var siteLocations = locations.Where(location => string.Equals(location.SiteId, siteId, StringComparison.OrdinalIgnoreCase)).ToArray();

        return PickByHost(siteLocations) ?? locations.FirstOrDefault(location => string.IsNullOrEmpty(location.SiteId));
    }

    private VirtualFileLocation? PickByHost(IReadOnlyCollection<VirtualFileLocation> locations)
    {
        var requestHost = _httpContextAccessor.HttpContext?.Request.Host.Value;
        var noHostMatch = locations.FirstOrDefault(location => string.IsNullOrEmpty(location.HostName));

        if (string.IsNullOrEmpty(requestHost))
        {
            return noHostMatch;
        }

        var hostMatch = locations.FirstOrDefault(location =>
            !string.IsNullOrEmpty(location.HostName) && string.Equals(location.HostName, requestHost, StringComparison.OrdinalIgnoreCase));
        var asteriskHostMatch = locations.FirstOrDefault(location => string.Equals(location.HostName, AsteriskHost, StringComparison.OrdinalIgnoreCase));

        return hostMatch ?? asteriskHostMatch ?? noHostMatch;
    }

    public PartialRouteData? GetPartialVirtualPath(VirtualTextRoutedData content, UrlGeneratorContext urlGeneratorContext)
    {
        if (string.IsNullOrWhiteSpace(content.SiteId) || string.IsNullOrEmpty(content.FileLocation?.VirtualPath))
        {
            return null;
        }

        if (_applicationRepository.Get(content.SiteId) is IRoutableApplication routableApplication)
        {
            return new PartialRouteData
            {
                BasePathRoot = routableApplication.RoutingEntryPoint,
                PartialVirtualPath = content.FileLocation.VirtualPath
            };
        }

        return null;
    }
}