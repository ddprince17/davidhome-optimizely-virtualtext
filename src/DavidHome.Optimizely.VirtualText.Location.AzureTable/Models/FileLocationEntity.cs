using Azure;
using Azure.Data.Tables;

namespace DavidHome.Optimizely.VirtualText.Location.AzureTable.Models;

internal class FileLocationEntity : ITableEntity
{
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string? SiteId { get; set; }
    public string? VirtualPath { get; set; }
}