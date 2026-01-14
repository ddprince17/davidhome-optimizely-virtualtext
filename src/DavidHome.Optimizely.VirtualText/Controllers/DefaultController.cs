using DavidHome.Optimizely.VirtualText.Plugin;
using DavidHome.Optimizely.VirtualText.Routing;
using EPiServer.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.Controllers;

[AuthorizePermission(PluginPermissions.GroupName, PluginPermissions.ViewPermissionsName)]
[Area("DhOpVirtualText")]
[ModuleRoute]
public class DefaultController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}