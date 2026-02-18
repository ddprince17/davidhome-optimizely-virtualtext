using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Models;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    private const string ModuleName = "DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt";
    
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddRobotsTxtExtension(IConfiguration configuration)
        {
            var configSection = configuration
                .GetSection(nameof(DavidHome))
                .GetSection(nameof(DavidHome.Optimizely.VirtualText));
            
            serviceBuilder.Services?
                .Configure<RobotsTxtVirtualTextOptions>(configSection)
                .Configure<ProtectedModuleOptions>(options =>
                {
                    if (!options.Items.Any(item => item.Name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        options.Items.Add(new ModuleDetails { Name = ModuleName });
                    }
                });

            return serviceBuilder.AddRobotsTxtCore();
        }
    }
}
