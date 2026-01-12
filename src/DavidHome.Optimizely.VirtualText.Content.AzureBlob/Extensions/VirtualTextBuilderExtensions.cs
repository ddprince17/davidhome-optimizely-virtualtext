using DavidHome.Optimizely.VirtualText.Content.AzureBlob;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextBuilderExtensions
{
    extension(IVirtualTextBuilder serviceBuilder)
    {
        public IVirtualTextBuilder AddAzureBlobContent(IConfigurationSection configuration)
        {
            serviceBuilder.Services?
                .AddTransient<IVirtualFileContentService, VirtualFileContentService>()
                .AddAzureClients(builder => builder.AddBlobServiceClient(configuration).WithName(VirtualTextConstants.ClientName));

            return serviceBuilder;
        }
    }
}