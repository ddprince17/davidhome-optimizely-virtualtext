namespace DavidHome.Optimizely.VirtualText.Models;

public record VirtualFileLocationQuery
{
    public string? VirtualPath { get; init; }
    public string? SiteId { get; init; }
    public int PageNumber { get; init; } = 1;
}