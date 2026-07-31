using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Options;

namespace DavidHome.Optimizely.VirtualText.Core.Services;

public class VirtualFileBridgeService : IVirtualFileBridgeService
{
    private readonly IVirtualFileContentService _fileContentService;
    private readonly IVirtualFileLocationService _fileLocationService;
    private readonly IOptionsMonitor<VirtualTextOptions> _virtualTextOptions;

    public VirtualFileBridgeService(IVirtualFileContentService fileContentService, IVirtualFileLocationService fileLocationService,
        IOptionsMonitor<VirtualTextOptions> virtualTextOptions)
    {
        _fileContentService = fileContentService;
        _fileLocationService = fileLocationService;
        _virtualTextOptions = virtualTextOptions;
    }

    public async Task<PagedResult<ContentServiceFile>> GetUnimportedFilesAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        return await GetUnimportedFilesInternalAsync(pageNumber, true, cancellationToken);
    }

    private async Task<PagedResult<ContentServiceFile>> GetUnimportedFilesInternalAsync(int pageNumber, bool peek, CancellationToken cancellationToken = default)
    {
        var items = new List<ContentServiceFile>();
        var currentPage = pageNumber;
        var hasMore = false;
        var maxItems = _virtualTextOptions.CurrentValue.MaxFileContentsPerPage;

        while (items.Count < maxItems)
        {
            var pagedResult = await _fileContentService.ListFilePathsAsync(currentPage, cancellationToken);
            hasMore = pagedResult.HasMore;

            var existingKeys = await GetExistingLocationKeys(pagedResult.Items, cancellationToken);
            var newItems = pagedResult.Items
                .Where(item => !existingKeys.Contains(GetLocationKey(item.VirtualPath, item.SourceSiteId, item.SourceHostName)))
                .ToArray();

            items.AddRange(newItems);

            if (!hasMore)
            {
                break;
            }

            currentPage++;
        }

        int? nextPageNumber = hasMore ? currentPage + 1 : null;
        var peekedPage = nextPageNumber != null && peek ? await GetUnimportedFilesInternalAsync(nextPageNumber.Value, false,  cancellationToken) : null;
        
        return new PagedResult<ContentServiceFile>
        {
            Items = items,
            HasMore = hasMore && peekedPage?.HasMore == true,
            NextPageNumber = nextPageNumber
        };
    }

    public async Task MoveAndUpsertLocationAsync(
        string virtualPath,
        string? sourceSiteId,
        string? sourceHostName,
        string? targetSiteId,
        string? targetHostName,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(sourceSiteId ?? string.Empty, targetSiteId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            await _fileContentService.MoveVirtualFileAsync(
                virtualPath,
                sourceSiteId,
                sourceHostName,
                targetSiteId,
                targetHostName,
                cancellationToken);
            var sourceHostNameArg = string.IsNullOrWhiteSpace(sourceSiteId) ? null : sourceHostName;
            await _fileLocationService.DeleteFileLocationAsync(virtualPath, sourceSiteId, sourceHostNameArg, cancellationToken);
        }

        var targetHostNameArg = string.IsNullOrWhiteSpace(targetSiteId) ? null : targetHostName;
        await _fileLocationService.UpsertFileLocationAsync(new VirtualFileLocation
        {
            SiteId = targetSiteId,
            HostName = targetHostNameArg,
            VirtualPath = virtualPath
        }, cancellationToken);
    }

    private async Task<HashSet<string>> GetExistingLocationKeys(IReadOnlyCollection<ContentServiceFile> items, CancellationToken cancellationToken)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        if (items.Count == 0)
        {
            return keys;
        }

        foreach (var group in items.GroupBy(item => new { SiteId = item.SourceSiteId ?? string.Empty, HostName = item.SourceHostName ?? string.Empty }))
        {
            var paths = group
                .Select(item => item.VirtualPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (paths.Length == 0)
            {
                continue;
            }

            var pagedResult = await _fileLocationService.QueryFileLocationsAsync(new VirtualFileLocationQuery
            {
                VirtualPaths = paths,
                SiteId = group.Key.SiteId,
                HostName = string.IsNullOrEmpty(group.Key.SiteId) ? string.Empty : group.Key.HostName
            }, cancellationToken);

            foreach (var item in pagedResult.Items)
            {
                keys.Add(GetLocationKey(item.VirtualPath, item.SiteId, item.HostName));
            }
        }

        return keys;
    }

    private static string GetLocationKey(string? virtualPath, string? siteId, string? hostName)
    {
        var hostKey = string.IsNullOrWhiteSpace(siteId) ? string.Empty : (hostName ?? string.Empty);
        return $"{siteId ?? string.Empty}::{hostKey}::{virtualPath}";
    }
}