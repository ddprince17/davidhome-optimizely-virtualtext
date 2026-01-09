using DavidHome.Optimizely.VirtualText;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Routing;
using DavidHome.Optimizely.VirtualText.Services;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public static class VirtualTextServiceCollectionExtensions
{
    internal const string ClientName = "DavidHomeVirtualText";

    public static IServiceCollection AddDavidHomeVirtualText(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton(typeof(VirtualTextPartialRouter<>))
            .AddSingleton(typeof(IVirtualTextPartialRouterWrapper<>), typeof(VirtualTextPartialRouterWrapper<>))
            .AddTransient<IVirtualFileLocationService, VirtualFileLocationService>()
            .AddTransient<IVirtualFileContentService, VirtualFileContentService>()
            .AddAzureClients(builder =>
            {
                builder.AddTableServiceClient(configuration).WithName(ClientName);
                builder.AddBlobServiceClient(configuration).WithName(ClientName);
            });

        return services;
    }
}