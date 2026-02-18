using DavidHome.Optimizely.VirtualText.Contracts;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class VirtualTextAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseRobotsTxtExtension()
        {
            return app.UseRobotsTxtCore();
        }
    }
}
