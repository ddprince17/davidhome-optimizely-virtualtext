namespace DavidHome.Optimizely.VirtualText.Models;

public class PagedResult<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public required bool HasMore { get; init; }
    public int? NextPageNumber { get; init; }
}
