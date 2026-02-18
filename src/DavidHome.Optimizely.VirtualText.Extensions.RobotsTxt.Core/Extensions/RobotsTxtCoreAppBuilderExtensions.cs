using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Middleware;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

internal static class RobotsTxtCoreAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseRobotsTxtCore()
        {
            app?.Builder?.UseMiddleware<RobotsNoIndexMiddleware>();
            
            return app;
        }
    }
}
