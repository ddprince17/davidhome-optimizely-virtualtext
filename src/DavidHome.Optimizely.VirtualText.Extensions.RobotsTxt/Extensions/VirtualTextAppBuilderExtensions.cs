using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Middleware;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class VirtualTextAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseRobotsTxtExtension()
        {
            app?.Builder?.UseMiddleware<RobotsNoIndexMiddleware>();
            return app;
        }
    }
}
