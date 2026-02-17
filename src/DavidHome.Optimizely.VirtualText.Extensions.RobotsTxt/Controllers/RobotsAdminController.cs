using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Services;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Models;
using DavidHome.Optimizely.VirtualText.Plugin;
using DavidHome.Optimizely.VirtualText.Routing;
using EPiServer.Security;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Controllers;

[AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.ViewPermissionsName, PluginPermissions.EditPermissionsName)]
[ModuleRoute]
public class RobotsAdminController : Controller
{
    private readonly IRobotsIndexingPolicyService _indexingPolicyService;
    private readonly PermissionService _permissionService;

    [ViewData] public string? Title { get; set; }

    public RobotsAdminController(IRobotsIndexingPolicyService indexingPolicyService, PermissionService permissionService)
    {
        _indexingPolicyService = indexingPolicyService;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Title = "Robots.txt Indexing";

        var environments = await _indexingPolicyService.ListVisibleEnvironmentsAsync(cancellationToken);
        var model = new RobotsTxtIndexViewModel
        {
            CurrentEnvironment = environments.FirstOrDefault(environment => environment.IsCurrent)?.EnvironmentName ?? string.Empty,
            Environments = environments,
            CanEdit = _permissionService.IsPermitted(User, PluginPermissions.EditSettings)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.EditPermissionsName)]
    public async Task<IActionResult> Save([FromForm] SaveRobotsEnvironmentSettingsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EnvironmentName))
        {
            return BadRequest("Environment name is required.");
        }

        try
        {
            await _indexingPolicyService.SaveEnvironmentSettingAsync(request.EnvironmentName, request.RobotsDirective, cancellationToken);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
