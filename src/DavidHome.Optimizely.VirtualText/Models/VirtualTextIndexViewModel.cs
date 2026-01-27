namespace DavidHome.Optimizely.VirtualText.Models;

public class VirtualTextIndexViewModel
{
    public IReadOnlyList<VirtualTextFileListItem> Files { get; init; } = [];
    public IReadOnlyList<VirtualTextSiteOption> Sites { get; init; } = [];
    public bool CanEdit { get; init; }
}

public class VirtualTextFileListItem
{
    public string VirtualPath { get; init; } = string.Empty;
    public string? SiteId { get; init; }
    public string? HostName { get; init; }
    public string SiteName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public class VirtualTextSiteOption
{
    public string? SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> Hosts { get; init; } = [];
}

public class VirtualTextFileListResponse
{
    public IReadOnlyList<VirtualTextFileListItem> Files { get; init; } = [];
    public bool HasMore { get; init; }
}
