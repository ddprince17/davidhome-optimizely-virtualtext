using System.Linq.Expressions;
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

public class TableFileLocationService : IVirtualFileLocationService
{
    internal const string TableName = "DhVirtualText";

    private readonly IAzureClientFactory<TableServiceClient> _tableClientFactory;
    private readonly IOptionsMonitor<VirtualTextOptions> _virtualTextOptions;

    private TableServiceClient FileLocationServiceClient => _tableClientFactory.CreateClient(VirtualTextConstants.ClientName);
    private TableClient FileLocationTableClient => FileLocationServiceClient.GetTableClient(TableName);

    public TableFileLocationService(IAzureClientFactory<TableServiceClient> tableClientFactory, IOptionsMonitor<VirtualTextOptions> virtualTextOptions)
    {
        _tableClientFactory = tableClientFactory;
        _virtualTextOptions = virtualTextOptions;
    }

    public async Task<PagedResult<VirtualFileLocation>> QueryFileLocationsAsync(VirtualFileLocationQuery query, CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var parameter = Expression.Parameter(typeof(FileLocationEntity), "entity");
        var predicateBody = BuildPredicateBody(query, parameter);

        if (predicateBody == null)
        {
            return new PagedResult<VirtualFileLocation>
            {
                Items = [],
                HasMore = false
            };
        }

        var predicate = Expression.Lambda<Func<FileLocationEntity, bool>>(predicateBody, parameter);
        var page = await FileLocationTableClient
            .QueryAsync(predicate, maxPerPage: maxPageSize, cancellationToken: cancellationToken)
            .AsPages(pageSizeHint: maxPageSize)
            .SelectPageNumber(query.PageNumber)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        var items = page?.Values.Select(entity => new VirtualFileLocation
        {
            SiteId = entity.SiteId,
            HostName = entity.HostName,
            VirtualPath = entity.VirtualPath
        }).ToArray() ?? [];

        return new PagedResult<VirtualFileLocation>
        {
            Items = items,
            HasMore = !string.IsNullOrEmpty(page?.ContinuationToken)
        };
    }

    public async Task<PagedResult<VirtualFileLocation>> QueryFileLocationsFuzzyAsync(VirtualFileLocationQuery query, CancellationToken cancellationToken = default)
    {
        var maxPageSize = _virtualTextOptions.CurrentValue.MaxFileLocationsPerPage;
        var skipCount = (query.PageNumber - 1) * maxPageSize;
        var entities = FileLocationTableClient.QueryAsync<FileLocationEntity>(
                entity => string.IsNullOrEmpty(query.SiteId) || entity.SiteId == query.SiteId,
                maxPerPage: maxPageSize,
                cancellationToken: cancellationToken)
            .Where(entity => string.IsNullOrEmpty(query.HostName) || entity.HostName == query.HostName)
            .Where(entity => query.VirtualPaths == null || query.VirtualPaths.Any(value => entity.VirtualPath?.Contains(value, StringComparison.Ordinal) ?? false))
            .Skip(skipCount)
            .Take(maxPageSize + 1);

        var rawItems = await entities.Select(entity => new VirtualFileLocation
        {
            SiteId = entity.SiteId,
            HostName = entity.HostName,
            VirtualPath = entity.VirtualPath
        }).ToArrayAsync(cancellationToken);
        var hasMore = rawItems.Length > maxPageSize;
        IReadOnlyCollection<VirtualFileLocation> items = hasMore ? new ArraySegment<VirtualFileLocation>(rawItems, 0, maxPageSize) : rawItems;

        return new PagedResult<VirtualFileLocation>
        {
            Items = items,
            HasMore = hasMore
        };
    }

    public async Task UpsertFileLocationAsync(VirtualFileLocation location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location.VirtualPath))
        {
            throw new ArgumentException("VirtualPath is required.", nameof(location));
        }

        var partitionKey = string.IsNullOrWhiteSpace(location.SiteId) ? "default" : location.SiteId;
        var rowKey = EncodeRowKey(location.VirtualPath, location.HostName);
        var entity = new FileLocationEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            SiteId = location.SiteId,
            HostName = location.HostName,
            VirtualPath = location.VirtualPath
        };

        await FileLocationTableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteFileLocationAsync(string virtualPath, string? siteId = null, string? hostName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            throw new ArgumentException("VirtualPath is required.", nameof(virtualPath));
        }

        var partitionKey = string.IsNullOrWhiteSpace(siteId) ? "default" : siteId;
        var rowKey = EncodeRowKey(virtualPath, hostName);

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
                HostName = entity.HostName,
                VirtualPath = entity.VirtualPath
            };
        }
    }

    private static string EncodeRowKey(string virtualPath, string? hostName = null)
    {
        var keyValue = string.IsNullOrEmpty(hostName)
            ? virtualPath
            : $"{hostName}::{virtualPath}";
        var bytes = Encoding.UTF8.GetBytes(keyValue);
        return Convert.ToBase64String(bytes)
            .Replace('/', '_')
            .Replace('+', '-');
    }

    private static Expression? BuildPredicateBody(VirtualFileLocationQuery query, ParameterExpression parameter)
    {
        Expression? predicateBody = null;
        var virtualPathProperty = Expression.Property(parameter, nameof(FileLocationEntity.VirtualPath));

        foreach (var path in query.VirtualPaths ?? [])
        {
            var equalsExpression = Expression.Equal(virtualPathProperty, Expression.Constant(path));

            predicateBody = predicateBody == null ? equalsExpression : Expression.OrElse(predicateBody, equalsExpression);
        }

        if (!string.IsNullOrEmpty(query.SiteId))
        {
            var siteIdProperty = Expression.Property(parameter, nameof(FileLocationEntity.SiteId));
            var siteEquals = Expression.Equal(siteIdProperty, Expression.Constant(query.SiteId));
            
            predicateBody = predicateBody == null ? siteEquals : Expression.AndAlso(predicateBody, siteEquals);
        }

        if (!string.IsNullOrEmpty(query.HostName))
        {
            var hostProperty = Expression.Property(parameter, nameof(FileLocationEntity.HostName));
            var hostEquals = Expression.Equal(hostProperty, Expression.Constant(query.HostName));
            
            predicateBody = predicateBody == null ? hostEquals : Expression.AndAlso(predicateBody, hostEquals);
        }

        return predicateBody;
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
