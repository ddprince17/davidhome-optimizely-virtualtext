using System.Runtime.CompilerServices;
using System.Text;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using DavidHome.Optimizely.VirtualText.Plugin;
using DavidHome.Optimizely.VirtualText.Routing;
using EPiServer.Web;
using Microsoft.AspNetCore.Mvc;

namespace DavidHome.Optimizely.VirtualText.Controllers;

[AnyAuthorizePermission(PluginPermissions.GroupName, PluginPermissions.ViewPermissionsName, PluginPermissions.EditPermissionsName)]
[ModuleRoute]
public class DefaultController : Controller
{
    private readonly IVirtualFileLocationService _fileLocationService;
    private readonly IVirtualFileContentService _fileContentService;
    private readonly ISiteDefinitionRepository _siteDefinitionRepository;

    public DefaultController(IVirtualFileLocationService fileLocationService, IVirtualFileContentService fileContentService, ISiteDefinitionRepository siteDefinitionRepository)
    {
        _fileLocationService = fileLocationService;
        _fileContentService = fileContentService;
        _siteDefinitionRepository = siteDefinitionRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var sites = GetVirtualTextSiteOptions().ToArray();
        var fileLocations = GetVirtualTextFileListItems(sites, cancellationToken);
        var model = new VirtualTextIndexViewModel
        {
            Files = await fileLocations
                .OrderBy(file => file.VirtualPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file.SiteName, StringComparer.OrdinalIgnoreCase)
                .ToArrayAsync(cancellationToken: cancellationToken),
            Sites = sites
        };

        return View(model);
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

    private async IAsyncEnumerable<VirtualTextFileListItem> GetVirtualTextFileListItems(IReadOnlyCollection<VirtualTextSiteOption> sites,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var location in _fileLocationService.GetAllFileLocations(cancellationToken: cancellationToken))
        {
            var siteName = string.IsNullOrWhiteSpace(location.SiteId)
                ? "Default (All Sites)"
                : sites.FirstOrDefault(site => site.SiteId == location.SiteId)?.Name ?? location.SiteId;

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

        var stream = await _fileContentService.GetVirtualFileContentAsync(virtualPath, siteId, cancellationToken);

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
}