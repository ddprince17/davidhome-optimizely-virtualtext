using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileBridgeService
{
    Task<PagedResult<ContentServiceFile>> GetUnimportedFilesAsync(int pageNumber, CancellationToken cancellationToken = default);
    Task MoveAndUpsertLocationAsync(
        string virtualPath,
        string? sourceSiteId,
        string? sourceHostName,
        string? targetSiteId,
        string? targetHostName,
        CancellationToken cancellationToken = default);
}
