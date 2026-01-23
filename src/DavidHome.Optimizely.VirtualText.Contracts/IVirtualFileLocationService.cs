using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts
{
    public interface IVirtualFileLocationService
    {
        IAsyncEnumerable<VirtualFileLocation> QueryFileLocations(VirtualFileLocationQuery query, CancellationToken cancellationToken = default);
        IAsyncEnumerable<VirtualFileLocation> GetAllFileLocations(int pageNumber = 1, CancellationToken cancellationToken = default);
        Task UpsertFileLocationAsync(VirtualFileLocation location, CancellationToken cancellationToken = default);
        Task DeleteFileLocationAsync(string virtualPath, string? siteId = null, CancellationToken cancellationToken = default);
    }
}
