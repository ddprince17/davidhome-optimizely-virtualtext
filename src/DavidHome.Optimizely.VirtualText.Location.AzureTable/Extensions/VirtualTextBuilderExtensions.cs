using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Location.AzureTable;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddAzureTableLocation(IConfigurationSection configuration)
        {
            serviceBuilder.Services?
                .AddTransient<IVirtualFileLocationService, VirtualFileLocationService>()
                .AddAzureClients(builder => builder.AddTableServiceClient(configuration).WithName(VirtualTextConstants.ClientName));

            return serviceBuilder;
        }
    }
}