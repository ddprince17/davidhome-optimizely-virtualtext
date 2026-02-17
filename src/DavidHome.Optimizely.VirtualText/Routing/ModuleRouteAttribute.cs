using EPiServer.Shell;
using Microsoft.AspNetCore.Mvc.Routing;

// ReSharper disable ReplaceWithFieldKeyword

namespace DavidHome.Optimizely.VirtualText.Routing;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class ModuleRouteAttribute : Attribute, IRouteTemplateProvider
{
    private readonly Type _moduleType;

    public ModuleRouteAttribute(Type moduleType)
    {
        _moduleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
    }

    public string Template => Paths.ToResource(_moduleType, "{controller}/{action}/{id?}");
    public int? Order { get; set; } = 0;
    public string? Name => _moduleType.Assembly.GetName().Name;
}
