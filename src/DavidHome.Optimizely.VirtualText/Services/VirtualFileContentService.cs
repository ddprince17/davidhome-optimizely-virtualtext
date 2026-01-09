using Azure.Storage.Blobs;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Exceptions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.Optimizely.VirtualText.Services;

public class VirtualFileContentService : IVirtualFileContentService
{
    internal const string BlobContainerName = "dhvirtualtext";
    private readonly IAzureClientFactory<BlobServiceClient> _blobServiceFactory;

    private BlobServiceClient BlobService => _blobServiceFactory.CreateClient(VirtualTextServiceCollectionExtensions.ClientName);
    private BlobContainerClient ContainerClient => BlobService.GetBlobContainerClient(BlobContainerName);

    public VirtualFileContentService(IAzureClientFactory<BlobServiceClient> blobServiceFactory)
    {
        _blobServiceFactory = blobServiceFactory;
    }

    public async Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, CancellationToken cancellationToken = default)
    {
        var blobPaths = new[] { siteId, virtualPath?.TrimStart('/').TrimEnd('/') }.Where(s => !string.IsNullOrEmpty(s));
        var blobName = string.Join('/', blobPaths);
        
        // Path is invalid and cannot be processed.
        if (string.IsNullOrEmpty(blobName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }
        
        var blob = ContainerClient.GetBlobClient(blobName);
        
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }
}