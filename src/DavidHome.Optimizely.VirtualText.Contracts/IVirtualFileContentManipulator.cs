namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileContentManipulator
{
    Task<Stream> TransformAsync(string virtualPath, string? siteId, string? hostName, Stream content, CancellationToken cancellationToken = default);
}
