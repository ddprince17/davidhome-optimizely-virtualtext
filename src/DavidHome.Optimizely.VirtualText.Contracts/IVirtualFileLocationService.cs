using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts
{
    public interface IVirtualFileLocationService
    {
        Task<PagedResult<VirtualFileLocation>> QueryFileLocationsAsync(VirtualFileLocationQuery query, CancellationToken cancellationToken = default);
        Task<PagedResult<VirtualFileLocation>> QueryFileLocationsFuzzyAsync(VirtualFileLocationQuery query, CancellationToken cancellationToken = default);
        Task UpsertFileLocationAsync(VirtualFileLocation location, CancellationToken cancellationToken = default);
        Task DeleteFileLocationAsync(string virtualPath, string? siteId = null, string? hostName = null, CancellationToken cancellationToken = default);
    }
}
