using System.Runtime.CompilerServices;
using System.Text;
using Azure;
using Azure.Data.Tables;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Location.AzureTable.Models;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace DavidHome.Optimizely.VirtualText.Location.AzureTable;

public class VirtualFileLocationService : IVirtualFileLocationService
{
    internal const string TableName = "DhVirtualText";

    private readonly IAzureClientFactory<TableServiceClient> _tableClientFactory;
    private readonly IOptionsMonitor<VirtualTextOptions> _virtualTextOptions;

    private TableServiceClient FileLocationServiceClient => _tableClientFactory.CreateClient(VirtualTextConstants.ClientName);
    private TableClient FileLocationTableClient => FileLocationServiceClient.GetTableClient(TableName);

    public VirtualFileLocationService(IAzureClientFactory<TableServiceClient> tableClientFactory, IOptionsMonitor<VirtualTextOptions> virtualTextOptions)
    {
        _tableClientFactory = tableClientFactory;
        _virtualTextOptions = virtualTextOptions;
    }

    public IAsyncEnumerable<VirtualFileLocation> QueryFileLocations(VirtualFileLocationQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.VirtualPath))
        {
            return GetAllFileLocations(query.PageNumber, cancellationToken);
        }

        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var tableQuery = FileLocationTableClient.QueryAsync<FileLocationEntity>(
            entity => entity.VirtualPath == query.VirtualPath &&
                      (string.IsNullOrWhiteSpace(query.SiteId) || entity.SiteId == query.SiteId),
            maxPerPage: maxPageSize,
            cancellationToken: cancellationToken);

        return ReadFileLocationsPage(tableQuery, query.PageNumber, cancellationToken);
    }

    public IAsyncEnumerable<VirtualFileLocation> GetAllFileLocations(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var query = FileLocationTableClient.QueryAsync<FileLocationEntity>(
            maxPerPage: maxPageSize,
            cancellationToken: cancellationToken);

        return ReadFileLocationsPage(query, pageNumber, cancellationToken);
    }

    public async Task UpsertFileLocationAsync(VirtualFileLocation location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location.VirtualPath))
        {
            throw new ArgumentException("VirtualPath is required.", nameof(location));
        }

        var partitionKey = string.IsNullOrWhiteSpace(location.SiteId) ? "default" : location.SiteId;
        var rowKey = EncodeRowKey(location.VirtualPath);
        var entity = new FileLocationEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            SiteId = location.SiteId,
            VirtualPath = location.VirtualPath
        };

        await FileLocationTableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteFileLocationAsync(string virtualPath, string? siteId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            throw new ArgumentException("VirtualPath is required.", nameof(virtualPath));
        }

        var partitionKey = string.IsNullOrWhiteSpace(siteId) ? "default" : siteId;
        var rowKey = EncodeRowKey(virtualPath);

        try
        {
            await FileLocationTableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            // Ignore missing entries.
        }
    }

    private async IAsyncEnumerable<VirtualFileLocation> ReadFileLocationsPage(AsyncPageable<FileLocationEntity> query, int pageNumber,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var page = query
            .AsPages(pageSizeHint: maxPageSize)
            .SelectPageNumber(pageNumber);
        var entities = await page
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        foreach (var entity in entities?.Values ?? [])
        {
            yield return new VirtualFileLocation
            {
                SiteId = entity.SiteId,
                VirtualPath = entity.VirtualPath
            };
        }
    }

    private static string EncodeRowKey(string virtualPath)
    {
        var bytes = Encoding.UTF8.GetBytes(virtualPath);
        return Convert.ToBase64String(bytes)
            .Replace('/', '_')
            .Replace('+', '-');
    }
}

file static class VirtualFileLocationServiceExtensions
{
    extension<T>(IAsyncEnumerable<Page<T>> tablePages)
    {
        public IAsyncEnumerable<Page<T>> SelectPageNumber(int pageNumber = 1)
        {
            return pageNumber < 2 ? tablePages : tablePages.Skip(--pageNumber);
        }
    }
}
