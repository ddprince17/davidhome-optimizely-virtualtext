using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using EPiServer.Shell.Modules;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextServiceCollectionExtensions
{
    private const string ModuleName = "DavidHome.Optimizely.VirtualText";

    extension(IServiceCollection services)
    {
        public IVirtualTextBuilder AddDavidHomeVirtualText(IConfiguration configuration)
        {
            var configSection = configuration
                .GetSection(nameof(DavidHome))
                .GetSection(nameof(DavidHome.Optimizely.VirtualText));
            
            return services
                .Configure<VirtualTextOptions>(configSection)
                .Configure<ProtectedModuleOptions>(options =>
                {
                    if (!options.Items.Any(item => item.Name.Equals(ModuleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        options.Items.Add(new ModuleDetails { Name = ModuleName });
                    }
                })
                .AddDavidHomeVirtualTextCore();
        }
    }
}