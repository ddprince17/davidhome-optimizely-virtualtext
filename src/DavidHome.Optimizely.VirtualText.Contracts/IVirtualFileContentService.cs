using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileContentService
{
    Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, string? hostName = null, CancellationToken cancellationToken = default);
    Task SaveVirtualFileContentAsync(string? virtualPath, string? siteId, string? hostName, Stream content, CancellationToken cancellationToken = default);
    Task DeleteVirtualFileContentAsync(string? virtualPath, string? siteId, string? hostName, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<ContentServiceFile> ListFilePaths(int pageNumber = 1, CancellationToken cancellationToken = default);
    Task MoveVirtualFileAsync(string? virtualPath, string? sourceSiteId, string? sourceHostName, string? targetSiteId, string? targetHostName, CancellationToken cancellationToken = default);
}
