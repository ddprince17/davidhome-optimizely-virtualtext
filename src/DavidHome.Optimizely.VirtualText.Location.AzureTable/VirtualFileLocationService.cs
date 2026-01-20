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

    public async IAsyncEnumerable<VirtualFileLocation> QueryFileLocations(string filePath, int pageNumber = 1,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var tables = FileLocationTableClient
            .QueryAsync<FileLocationEntity>(entity => entity.VirtualPath == filePath, maxPerPage: maxPageSize, cancellationToken: cancellationToken);
        var tablePage = tables
            .AsPages(pageSizeHint: maxPageSize)
            .SelectPageNumber(pageNumber);
        
        var fileLocationEntities = await tablePage.FirstOrDefaultAsync(cancellationToken: cancellationToken);

        foreach (var fileLocationEntity in fileLocationEntities?.Values ?? [])
        {
            yield return new VirtualFileLocation
            {
                SiteId = fileLocationEntity.SiteId,
                VirtualPath = fileLocationEntity.VirtualPath
            };
        }
    }

    public async IAsyncEnumerable<VirtualFileLocation> GetAllFileLocations(int pageNumber = 1, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var tables = FileLocationTableClient.QueryAsync<FileLocationEntity>(maxPerPage: maxPageSize, cancellationToken: cancellationToken);
        var tablePage = tables
            .AsPages(pageSizeHint: maxPageSize)
            .SelectPageNumber(pageNumber);

        var fileLocationEntities = await tablePage.FirstOrDefaultAsync(cancellationToken: cancellationToken);

        foreach (var fileLocationEntity in fileLocationEntities?.Values ?? [])
        {
            yield return new VirtualFileLocation
            {
                SiteId = fileLocationEntity.SiteId,
                VirtualPath = fileLocationEntity.VirtualPath
            };
        }
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
