namespace DavidHome.Optimizely.VirtualText.Models;

public class VirtualTextImportViewModel
{
    public IReadOnlyList<VirtualTextImportItem> Items { get; init; } = [];
    public IReadOnlyList<VirtualTextSiteOption> Sites { get; init; } = [];
    public bool CanEdit { get; init; }
}

public class VirtualTextImportListResponse
{
    public IReadOnlyList<VirtualTextImportItem> Items { get; init; } = [];
    public bool HasMore { get; init; }
}
