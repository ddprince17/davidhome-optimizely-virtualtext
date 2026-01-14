using DavidHome.Optimizely.VirtualText.Plugin;
using DavidHome.Optimizely.VirtualText.Routing;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.Controllers;

[AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.ViewPermissionsName, PluginPermissions.EditPermissionsName)]
[ModuleRoute]
public class DefaultController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}