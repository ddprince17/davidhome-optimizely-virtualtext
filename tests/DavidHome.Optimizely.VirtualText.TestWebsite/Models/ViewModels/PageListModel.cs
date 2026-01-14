using DavidHome.Optimizely.VirtualText.TestWebsite.Models.Blocks;

namespace DavidHome.Optimizely.VirtualText.TestWebsite.Models.ViewModels;

public class PageListModel
{
    public PageListModel(PageListBlock block)
    {
        Heading = block.Heading;
        ShowIntroduction = block.IncludeIntroduction;
        ShowPublishDate = block.IncludePublishDate;
    }

    public string Heading { get; set; }

    public IEnumerable<PageData> Pages { get; set; }

    public bool ShowIntroduction { get; set; }

    public bool ShowPublishDate { get; set; }
}