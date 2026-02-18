using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Controllers;
using EPiServer.Framework.Localization;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Plugin;

[MenuProvider]
public class RobotsTxtPluginMenuProvider : IMenuProvider
{
    private readonly LocalizationService _localizationService;

    public RobotsTxtPluginMenuProvider(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public IEnumerable<MenuItem> GetMenuItems()
    {
        var moduleType = GetType();

        yield return new UrlMenuItem(_localizationService.GetString("/davidhome/dhopvirtualtext/robotstxt/menu", "Robots.txt"), "/global/cms/dhopvirtualtext/robotstxt",
            Paths.ToResource(moduleType, $"RobotsAdmin/{nameof(RobotsAdminController.Index)}"))
        {
            SortIndex = 30,
            Alignment = 0,
            IsAvailable = _ => true
        };
    }
}
