using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Storage.AzureTable;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class RobotsTxtBuilderExtensions
{
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddAzureTableRobotsTxtStorage(IConfigurationSection configuration)
        {
            serviceBuilder.Services?
                .AddTransient<IRobotsEnvironmentIndexingSettingsStore, TableRobotsEnvironmentIndexingSettingsStore>()
                .AddAzureClients(builder => builder.AddTableServiceClient(configuration).WithName(RobotsTxtConstants.ClientName));

            return serviceBuilder;
        }
    }
}
