using EPiServer.Shell;
using Microsoft.AspNetCore.Mvc.Routing;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Routing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ModuleRoute : Attribute, IRouteTemplateProvider
{
    public string Template => Paths.ToResource(typeof(ModuleRoute), "{controller}/{action}/{id?}");
    public int? Order { get; set; } = 0;
    public string? Name { get; set; } = "RobotsTxtPluginModuleRoute";
}
