using Azure.Storage.Blobs;
using DavidHome.Optimizely.VirtualText.Content.AzureBlob.Exceptions;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;

namespace DavidHome.Optimizely.VirtualText.Content.AzureBlob;

public class VirtualFileContentService : IVirtualFileContentService
{
    internal const string BlobContainerName = "dhvirtualtext";
    private readonly IAzureClientFactory<BlobServiceClient> _blobServiceFactory;

    private BlobServiceClient BlobService => _blobServiceFactory.CreateClient(VirtualTextConstants.ClientName);
    private BlobContainerClient ContainerClient => BlobService.GetBlobContainerClient(BlobContainerName);

    public VirtualFileContentService(IAzureClientFactory<BlobServiceClient> blobServiceFactory)
    {
        _blobServiceFactory = blobServiceFactory;
    }

    public async Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(virtualPath, siteId);
        
        // Path is invalid and cannot be processed.
        if (string.IsNullOrEmpty(blobName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }
        
        var blob = ContainerClient.GetBlobClient(blobName);
        
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task SaveVirtualFileContentAsync(string? virtualPath, string? siteId, Stream content, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(virtualPath, siteId);

        if (string.IsNullOrEmpty(blobName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }
        
        var blob = ContainerClient.GetBlobClient(blobName);
        
        await blob.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken);
    }

    private static string GetBlobName(string? virtualPath, string? siteId)
    {
        var blobPaths = new[] { siteId, virtualPath?.TrimStart('/').TrimEnd('/') }.Where(s => !string.IsNullOrEmpty(s));
        var blobName = string.Join('/', blobPaths);
        
        return blobName;
    }
}
