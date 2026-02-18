using System.Runtime.CompilerServices;
using Azure;
using Azure.Storage.Blobs;
using DavidHome.Optimizely.VirtualText.Content.AzureBlob.Exceptions;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace DavidHome.Optimizely.VirtualText.Content.AzureBlob;

public class BlobFileContentService : IVirtualFileContentService
{
    internal const string BlobContainerName = "dhvirtualtext";
    private readonly IAzureClientFactory<BlobServiceClient> _blobServiceFactory;
    private readonly IOptionsMonitor<VirtualTextOptions> _virtualTextOptions;

    private BlobServiceClient BlobService => _blobServiceFactory.CreateClient(VirtualTextConstants.ClientName);
    private BlobContainerClient ContainerClient => BlobService.GetBlobContainerClient(BlobContainerName);

    public BlobFileContentService(IAzureClientFactory<BlobServiceClient> blobServiceFactory, IOptionsMonitor<VirtualTextOptions> virtualTextOptions)
    {
        _blobServiceFactory = blobServiceFactory;
        _virtualTextOptions = virtualTextOptions;
    }

    public async Task<Stream?> GetVirtualFileContentAsync(string? virtualPath, string? siteId = null, string? hostName = null, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(virtualPath, siteId, hostName);

        // Path is invalid and cannot be processed.
        if (string.IsNullOrEmpty(blobName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }

        var blob = ContainerClient.GetBlobClient(blobName);

        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task SaveVirtualFileContentAsync(string? virtualPath, string? siteId, string? hostName, Stream content, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(virtualPath, siteId, hostName);

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

    public async Task DeleteVirtualFileContentAsync(string? virtualPath, string? siteId, string? hostName, CancellationToken cancellationToken = default)
    {
        var blobName = GetBlobName(virtualPath, siteId, hostName);

        if (string.IsNullOrEmpty(blobName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }

        var blob = ContainerClient.GetBlobClient(blobName);

        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<ContentServiceFile> ListFilePaths(int pageNumber = 1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileContentsPerPage;
        var blobItems = await ContainerClient
            .GetBlobsAsync(cancellationToken: cancellationToken)
            .AsPages(pageSizeHint: maxPageSize)
            .SelectPageNumber(pageNumber)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        foreach (var blobItem in blobItems?.Values ?? [])
        {
            yield return BuildImportPath(blobItem.Name);
        }
    }

    public async Task MoveVirtualFileAsync(string? virtualPath, string? sourceSiteId, string? sourceHostName, string? targetSiteId, string? targetHostName, CancellationToken cancellationToken = default)
    {
        var sourceName = GetBlobName(virtualPath, sourceSiteId, sourceHostName);
        var targetName = GetBlobName(virtualPath, targetSiteId, targetHostName);

        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName))
        {
            throw new VirtualFilePathInvalidException("The virtual file path is invalid.");
        }

        if (string.Equals(sourceName, targetName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sourceBlob = ContainerClient.GetBlobClient(sourceName);
        var targetBlob = ContainerClient.GetBlobClient(targetName);

        var copyOperation = await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: cancellationToken);
        await copyOperation.WaitForCompletionAsync(cancellationToken);
        await sourceBlob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private static string GetBlobName(string? virtualPath, string? siteId, string? hostName = null)
    {
        var blobPaths = new[] { siteId, hostName, virtualPath?.TrimStart('/').TrimEnd('/') }.Where(s => !string.IsNullOrEmpty(s));
        var blobName = string.Join('/', blobPaths);

        return blobName;
    }

    private static ContentServiceFile BuildImportPath(string blobPath)
    {
        var segments = blobPath.Split('/', 3, StringSplitOptions.RemoveEmptyEntries);
        var potentialSite = segments[0];
        if (segments.Length <= 1 || !Guid.TryParse(potentialSite, out var siteGuid))
        {
            return new ContentServiceFile
            {
                VirtualPath = blobPath,
                SourceSiteId = null
            };
        }

        var normalizedSiteId = siteGuid.ToString("N");
        var virtualPath = segments.Length >= 3 ? segments[2] : segments[1];
        var hostName = segments.Length >= 3 ? segments[1] : null;

        return new ContentServiceFile
        {
            VirtualPath = virtualPath,
            SourceSiteId = normalizedSiteId,
            SourceHostName = hostName
        };
    }
}
file static class VirtualFileContentServiceExtensions
{
    extension<T>(IAsyncEnumerable<Page<T>> tablePages)
    {
        public IAsyncEnumerable<Page<T>> SelectPageNumber(int pageNumber = 1)
        {
            return pageNumber < 2 ? tablePages : tablePages.Skip(--pageNumber);
        }
    }
}
