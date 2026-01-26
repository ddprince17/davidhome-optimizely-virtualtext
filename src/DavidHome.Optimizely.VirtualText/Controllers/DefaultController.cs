using System.Runtime.CompilerServices;
using System.Text;
using Azure;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using DavidHome.Optimizely.VirtualText.Plugin;
using DavidHome.Optimizely.VirtualText.Routing;
using EPiServer.Web;
using EPiServer.Security;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.Controllers;

[AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.ViewPermissionsName, PluginPermissions.EditPermissionsName)]
[ModuleRoute]
public class DefaultController : Controller
{
    private readonly IVirtualFileLocationService _fileLocationService;
    private readonly IVirtualFileContentService _fileContentService;
    private readonly ISiteDefinitionRepository _siteDefinitionRepository;
    private readonly PermissionService _permissionService;

    public DefaultController(IVirtualFileLocationService fileLocationService, IVirtualFileContentService fileContentService, ISiteDefinitionRepository siteDefinitionRepository, PermissionService permissionService)
    {
        _fileLocationService = fileLocationService;
        _fileContentService = fileContentService;
        _siteDefinitionRepository = siteDefinitionRepository;
        _permissionService = permissionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sites = GetVirtualTextSiteOptions().ToArray();
        var fileLocations = GetVirtualTextFileListItems(sites, cancellationToken: cancellationToken);
        var model = new VirtualTextIndexViewModel
        {
            Files = await fileLocations
                .OrderBy(file => file.VirtualPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.SiteName, StringComparer.OrdinalIgnoreCase)
                .ToArrayAsync(cancellationToken: cancellationToken),
            Sites = sites,
            CanEdit = _permissionService.IsPermitted(User, PluginPermissions.EditSettings)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FileList(string? virtualPath, string? siteId, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be at least 1.");
        }

        var sites = GetVirtualTextSiteOptions().ToArray();
        var files = await GetVirtualTextFileListItems(sites, virtualPath, siteId, pageNumber, cancellationToken)
            .ToArrayAsync(cancellationToken: cancellationToken);

        return Json(new VirtualTextFileListResponse
        {
            Files = files,
            HasMore = files.Length > 0
        });
    }

    private IEnumerable<VirtualTextSiteOption> GetVirtualTextSiteOptions()
    {
        yield return new VirtualTextSiteOption
        {
            SiteId = null,
            Name = "Default (All Sites)"
        };

        foreach (var site in _siteDefinitionRepository.List().OrderBy(site => site.Name))
        {
            yield return new VirtualTextSiteOption
            {
                SiteId = site.Id.ToString("N"),
                Name = site.Name
            };
        }
    }

    private async IAsyncEnumerable<VirtualTextFileListItem> GetVirtualTextFileListItems(
        IReadOnlyCollection<VirtualTextSiteOption> sites,
        string? virtualPath = null,
        string? siteId = null,
        int pageNumber = 1,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var locations = _fileLocationService.QueryFileLocationsFuzzy(new VirtualFileLocationQuery
        {
            VirtualPath = virtualPath,
            SiteId = siteId,
            PageNumber = pageNumber
        }, cancellationToken);

        await foreach (var location in locations)
        {
            var siteName = string.IsNullOrWhiteSpace(location.SiteId)
                ? "Default (All Sites)"
                : sites.FirstOrDefault(site => site.SiteId == location.SiteId)?.Name ?? "Unknown";

            yield return new VirtualTextFileListItem
            {
                VirtualPath = location.VirtualPath ?? string.Empty,
                SiteId = location.SiteId,
                SiteName = siteName,
                IsDefault = string.IsNullOrWhiteSpace(location.SiteId)
            };
        }
    }

    [HttpGet]
    public async Task<IActionResult> FileContent(string virtualPath, string? siteId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            return BadRequest("Virtual path is required.");
        }

        Stream? stream;
        try
        {
            stream = await _fileContentService.GetVirtualFileContentAsync(virtualPath, siteId, cancellationToken);
        }
        catch (RequestFailedException e)
        {
            if (e.Status == 404)
            {
                return NotFound();
            }

            throw;
        }

        if (stream == null)
        {
            return NotFound();
        }
        
        return new FileStreamResult(stream, "text/plain");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.EditPermissionsName)]
    public async Task<IActionResult> SaveFile([FromBody] SaveFileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VirtualPath))
        {
            return BadRequest("Virtual path is required.");
        }

        var contentBytes = Encoding.UTF8.GetBytes(request.Content ?? string.Empty);
        await using var contentStream = new MemoryStream(contentBytes);
        await _fileContentService.SaveVirtualFileContentAsync(request.VirtualPath, request.SiteId, contentStream, cancellationToken);
        await _fileLocationService.UpsertFileLocationAsync(new VirtualFileLocation
        {
            SiteId = request.SiteId,
            VirtualPath = request.VirtualPath
        }, cancellationToken);

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.EditPermissionsName)]
    public async Task<IActionResult> CopyFile([FromBody] CopyFileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VirtualPath))
        {
            return BadRequest("Virtual path is required.");
        }

        var sourceStream = await _fileContentService.GetVirtualFileContentAsync(request.VirtualPath, request.SourceSiteId, cancellationToken);
        if (sourceStream == null)
        {
            return NotFound();
        }

        await _fileContentService.SaveVirtualFileContentAsync(request.VirtualPath, request.TargetSiteId, sourceStream, cancellationToken);
        await _fileLocationService.UpsertFileLocationAsync(new VirtualFileLocation
        {
            SiteId = request.TargetSiteId,
            VirtualPath = request.VirtualPath
        }, cancellationToken);

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.EditPermissionsName)]
    public async Task<IActionResult> DeleteFile([FromBody] DeleteFileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VirtualPath))
        {
            return BadRequest("Virtual path is required.");
        }

        await _fileContentService.DeleteVirtualFileContentAsync(request.VirtualPath, request.SiteId, cancellationToken);
        await _fileLocationService.DeleteFileLocationAsync(request.VirtualPath, request.SiteId, cancellationToken);

        return Ok();
    }

    public class SaveFileRequest
    {
        public string VirtualPath { get; init; } = string.Empty;
        public string? SiteId { get; init; }
        public string? Content { get; init; }
    }

    public class CopyFileRequest
    {
        public string VirtualPath { get; init; } = string.Empty;
        public string? SourceSiteId { get; init; }
        public string? TargetSiteId { get; init; }
    }

    public class DeleteFileRequest
    {
        public string VirtualPath { get; init; } = string.Empty;
        public string? SiteId { get; init; }
    }
}
