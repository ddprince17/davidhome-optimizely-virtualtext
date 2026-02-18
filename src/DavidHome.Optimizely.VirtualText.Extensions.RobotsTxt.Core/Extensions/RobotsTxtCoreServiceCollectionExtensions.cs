using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Middleware;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Services;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

internal static class RobotsTxtCoreServiceCollectionExtensions
{
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddRobotsTxtCore()
        {
            serviceBuilder.Services?
                .AddTransient<IRobotsIndexingPolicyService, RobotsIndexingPolicyService>()
                .AddTransient<IVirtualFileContentManipulator, RobotsTxtVirtualFileContentManipulator>()
                .AddTransient<RobotsNoIndexMiddleware>();
            
            return serviceBuilder;
        }
    }
}
