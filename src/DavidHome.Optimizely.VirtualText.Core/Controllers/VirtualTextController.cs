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
    private readonly IEnumerable<IVirtualFileContentManipulator> _contentManipulators;

    public VirtualTextController(
        IVirtualFileContentService fileContentService,
        IEnumerable<IVirtualFileContentManipulator> contentManipulators)
    {
        _fileContentService = fileContentService;
        _contentManipulators = contentManipulators;
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

        var fileContent = await _fileContentService.GetVirtualFileContentAsync(
            virtualTextRoutedData.FileLocation.VirtualPath,
            virtualTextRoutedData.FileLocation.SiteId,
            virtualTextRoutedData.FileLocation.HostName,
            HttpContext.RequestAborted);

        if (fileContent == null)
        {
            return NotFound();
        }

        var resolvedContent = await ApplyApplicableContentTransforms(fileContent, virtualTextRoutedData);

        return new FileStreamResult(resolvedContent, "text/plain");
    }

    private async Task<Stream> ApplyApplicableContentTransforms(Stream fileContent, VirtualTextRoutedData virtualTextRoutedData)
    {
        var resolvedContent = fileContent;
        
        foreach (var manipulator in _contentManipulators)
        {
            if (resolvedContent.CanSeek)
            {
                resolvedContent.Position = 0;
            }

            var transformedContent = await manipulator.TransformAsync(
                virtualTextRoutedData.FileLocation?.VirtualPath ?? string.Empty,
                virtualTextRoutedData.FileLocation?.SiteId,
                virtualTextRoutedData.FileLocation?.HostName,
                resolvedContent,
                HttpContext.RequestAborted);

            if (!ReferenceEquals(transformedContent, resolvedContent))
            {
                await resolvedContent.DisposeAsync();
            }

            resolvedContent = transformedContent;
        }

        if (resolvedContent.CanSeek)
        {
            resolvedContent.Position = 0;
        }

        return resolvedContent;
    }
}
