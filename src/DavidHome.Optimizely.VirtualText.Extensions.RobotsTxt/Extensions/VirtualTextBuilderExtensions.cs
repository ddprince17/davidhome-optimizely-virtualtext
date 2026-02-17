using DavidHome.Optimizely.VirtualText.Contracts;
using EPiServer.Shell.Modules;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    internal const string ModuleName = "DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt";
    
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddRobotsTxtExtension()
        {
            serviceBuilder.Services?
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
