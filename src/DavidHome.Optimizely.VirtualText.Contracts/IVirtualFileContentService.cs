namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileContentService
{
    Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, CancellationToken cancellationToken = default);
    Task SaveVirtualFileContentAsync(string? virtualPath, string? siteId, Stream content, CancellationToken cancellationToken = default);
}
