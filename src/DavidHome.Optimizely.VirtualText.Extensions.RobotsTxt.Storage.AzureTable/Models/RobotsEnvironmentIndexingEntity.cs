using Azure;
using Azure.Data.Tables;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable.Models;

internal class RobotsEnvironmentIndexingEntity : ITableEntity
{
    public const string Partition = "RobotsEnvironmentIndexing";

    public string PartitionKey { get; set; } = Partition;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string EnvironmentName { get; set; } = string.Empty;
    public string? RobotsDirective { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
