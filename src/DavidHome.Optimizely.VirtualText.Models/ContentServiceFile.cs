namespace DavidHome.Optimizely.VirtualText.Models;

public class ContentServiceFile
{
    public string VirtualPath { get; init; } = string.Empty;
    public string? SourceSiteId { get; init; }
    public string? SourceHostName { get; init; }
}
