using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Web;
using EPiServer.Web.Routing.Matching;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable DuplicatedSequentialIfBodies

namespace DavidHome.Optimizely.VirtualText.Core.Controllers;

public class VirtualTextController : Controller, IRenderTemplate<VirtualTextRoutedData>
{
    private readonly IVirtualFileContentService _fileContentService;

    public VirtualTextController(IVirtualFileContentService fileContentService)
    {
        _fileContentService = fileContentService;
    }

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Features.Get<IContentRouteFeature>()?.RoutedContentData.PartialRoutedObject is not VirtualTextRoutedData virtualTextRoutedData)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(virtualTextRoutedData.SiteId) || string.IsNullOrEmpty(virtualTextRoutedData.FileLocation?.VirtualPath))
        {
            return NotFound();
        }

        var fileContent = await _fileContentService.GetVirtualFileContentAsync(virtualTextRoutedData.FileLocation.VirtualPath, virtualTextRoutedData.FileLocation.SiteId);

        if (fileContent == null)
        {
            return NotFound();
        }

        return new FileStreamResult(fileContent, "text/plain");
    }
}