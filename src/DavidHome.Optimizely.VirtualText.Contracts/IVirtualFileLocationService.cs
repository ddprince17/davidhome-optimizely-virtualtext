using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualFileLocationService
{
    IAsyncEnumerable<VirtualFileLocation> QueryFileLocations(string filePath, int pageNumber = 1, CancellationToken cancellationToken = default);

    IAsyncEnumerable<VirtualFileLocation> GetAllFileLocations(int pageNumber = 1, CancellationToken cancellationToken = default);
}