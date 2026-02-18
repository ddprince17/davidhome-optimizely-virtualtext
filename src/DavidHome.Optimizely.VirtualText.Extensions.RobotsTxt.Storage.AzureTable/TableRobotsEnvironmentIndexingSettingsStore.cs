using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable.Models;
using Microsoft.Extensions.Azure;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable;

public class TableRobotsEnvironmentIndexingSettingsStore : IRobotsEnvironmentIndexingSettingsStore
{
    private readonly IAzureClientFactory<TableServiceClient> _tableClientFactory;

    private TableServiceClient TableServiceClient => _tableClientFactory.CreateClient(RobotsTxtConstants.ClientName);

    public TableRobotsEnvironmentIndexingSettingsStore(IAzureClientFactory<TableServiceClient> tableClientFactory)
    {
        _tableClientFactory = tableClientFactory;
    }

    public async Task<RobotsEnvironmentIndexingSetting?> GetAsync(string environmentName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return null;
        }

        var tableClient = TableServiceClient.GetTableClient(RobotsTxtConstants.TableName);

        try
        {
            var response = await tableClient.GetEntityAsync<RobotsEnvironmentIndexingEntity>(
                RobotsEnvironmentIndexingEntity.Partition,
                GetRowKey(environmentName),
                cancellationToken: cancellationToken);

            return response.Value.ToSetting();
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    public async IAsyncEnumerable<RobotsEnvironmentIndexingSetting> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var tableClient = TableServiceClient.GetTableClient(RobotsTxtConstants.TableName);
        var query = tableClient.QueryAsync<RobotsEnvironmentIndexingEntity>(
            entity => entity.PartitionKey == RobotsEnvironmentIndexingEntity.Partition,
            cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            yield return entity.ToSetting();
        }
    }

    public Task UpsertAsync(RobotsEnvironmentIndexingSetting setting, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);

        if (string.IsNullOrWhiteSpace(setting.EnvironmentName))
        {
            throw new ArgumentException("Environment name is required.", nameof(setting));
        }

        var tableClient = TableServiceClient.GetTableClient(RobotsTxtConstants.TableName);

        var entity = new RobotsEnvironmentIndexingEntity
        {
            RowKey = GetRowKey(setting.EnvironmentName),
            EnvironmentName = setting.EnvironmentName.Trim(),
            RobotsDirective = setting.RobotsDirective,
            UpdatedUtc = setting.UpdatedUtc
        };

        return tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken);
    }

    public async Task DeleteAsync(string environmentName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return;
        }

        var tableClient = TableServiceClient.GetTableClient(RobotsTxtConstants.TableName);

        try
        {
            await tableClient.DeleteEntityAsync(
                RobotsEnvironmentIndexingEntity.Partition,
                GetRowKey(environmentName),
                cancellationToken: cancellationToken);
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            // Already absent; nothing to reset.
        }
    }

    private static string GetRowKey(string environmentName) => environmentName.Trim().ToLowerInvariant();
}

internal static class RobotsEnvironmentIndexingEntityExtensions
{
    public static RobotsEnvironmentIndexingSetting ToSetting(this RobotsEnvironmentIndexingEntity entity)
    {
        return new RobotsEnvironmentIndexingSetting
        {
            EnvironmentName = entity.EnvironmentName,
            RobotsDirective = entity.RobotsDirective,
            UpdatedUtc = entity.UpdatedUtc
        };
    }
}
