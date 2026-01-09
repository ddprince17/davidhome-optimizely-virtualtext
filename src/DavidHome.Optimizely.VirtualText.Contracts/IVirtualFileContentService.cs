namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileContentService
{
    Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, CancellationToken cancellationToken = default);
}