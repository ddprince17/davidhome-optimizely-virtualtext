using Azure.Data.Tables;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class RobotsTxtAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseAzureTableRobotsTxtStorage()
        {
            var clientFactory = app?.Builder?.ApplicationServices.GetRequiredService<IAzureClientFactory<TableServiceClient>>();
            var tableClient = clientFactory?.CreateClient(RobotsTxtConstants.ClientName)?.GetTableClient(RobotsTxtConstants.TableName);

            tableClient?.CreateIfNotExists();

            return app;
        }
    }
}
