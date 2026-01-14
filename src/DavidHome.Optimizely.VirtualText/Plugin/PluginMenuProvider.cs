using DavidHome.Optimizely.VirtualText.Controllers;
using EPiServer.Framework.Localization;
using EPiServer.Shell;
using EPiServer.Shell.Navigation;

namespace DavidHome.Optimizely.VirtualText.Plugin;

[MenuProvider]
public class PluginMenuProvider : IMenuProvider
{
    private readonly LocalizationService _localizationService;

    public PluginMenuProvider(LocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public IEnumerable<MenuItem> GetMenuItems()
    {
        yield return new UrlMenuItem(_localizationService.GetString("/davidhome/dhopvirtualtext/gadget/title", "Virtual Text Settings"), "/global/cms/dhopvirtualtext",
            Paths.ToResource(GetType(), $"Default/{nameof(DefaultController.Index)}"))
        {
            SortIndex = 0,
            Alignment = 0,
            IsAvailable = _ => true
        };

        yield return new UrlMenuItem(_localizationService.GetString("/davidhome/dhopvirtualtext/index/menu", "Home"), "/global/cms/dhopvirtualtext/index",
            Paths.ToResource(GetType(), $"Default/{nameof(DefaultController.Index)}"))
        {
            SortIndex = 10,
            Alignment = 0,
            IsAvailable = _ => true
        };
    }
}