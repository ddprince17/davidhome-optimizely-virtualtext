using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts
{
    public interface IVirtualFileLocationService
    {
        IAsyncEnumerable<VirtualFileLocation> QueryFileLocations(VirtualFileLocationQuery query, CancellationToken cancellationToken = default);
        IAsyncEnumerable<VirtualFileLocation> QueryFileLocationsFuzzy(VirtualFileLocationQuery query, CancellationToken cancellationToken = default);
        Task UpsertFileLocationAsync(VirtualFileLocation location, CancellationToken cancellationToken = default);
        Task DeleteFileLocationAsync(string virtualPath, string? siteId = null, string? hostName = null, CancellationToken cancellationToken = default);
    }
}
