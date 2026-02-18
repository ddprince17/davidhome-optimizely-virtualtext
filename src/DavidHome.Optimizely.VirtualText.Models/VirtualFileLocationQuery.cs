namespace DavidHome.Optimizely.VirtualText.Models;

public record VirtualFileLocationQuery
{
    public IReadOnlyCollection<string>? VirtualPaths { get; init; }
    public string? SiteId { get; init; }
    public string? HostName { get; init; }
    public int PageNumber { get; init; } = 1;
}
