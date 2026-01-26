namespace DavidHome.Optimizely.VirtualText.Models;

public class VirtualTextImportItem
{
    public string VirtualPath { get; init; } = string.Empty;
    public string? SourceSiteId { get; init; }
    public string SourceSiteName { get; init; } = string.Empty;
    public bool IsUnknownSite { get; init; }
    public string? SelectedSiteId { get; init; }
}