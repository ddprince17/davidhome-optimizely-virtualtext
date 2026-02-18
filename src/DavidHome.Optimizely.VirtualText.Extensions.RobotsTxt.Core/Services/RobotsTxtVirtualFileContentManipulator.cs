using System.Text;
using DavidHome.Optimizely.VirtualText.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Services;

internal sealed class RobotsTxtVirtualFileContentManipulator : IVirtualFileContentManipulator
{
    private const string RobotsFileName = "robots.txt";
    private const string DisallowAllContent = "User-agent: *\nDisallow: /\n";
    private readonly IWebHostEnvironment _webHostEnvironment;

    public RobotsTxtVirtualFileContentManipulator(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public Task<Stream> TransformAsync(string virtualPath, string? siteId, string? hostName, Stream content, CancellationToken cancellationToken = default)
    {
        if (_webHostEnvironment.IsProduction() || !RobotsFileName.Equals(virtualPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(content);
        }

        Stream transformed = new MemoryStream(Encoding.UTF8.GetBytes(DisallowAllContent));

        return Task.FromResult(transformed);
    }
}