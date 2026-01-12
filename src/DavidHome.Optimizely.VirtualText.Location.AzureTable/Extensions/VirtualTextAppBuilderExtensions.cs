using Azure.Data.Tables;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Location.AzureTable;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class VirtualTextAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseAzureTableFileLocation()
        {
            var clientFactory = app?.Builder?.ApplicationServices.GetRequiredService<IAzureClientFactory<TableServiceClient>>();
            var tableClient = clientFactory?.CreateClient(VirtualTextConstants.ClientName);

            // Is making sure the table is created while starting the app.
            tableClient?.CreateTableIfNotExists(VirtualFileLocationService.TableName);

            return app;
        }
    }
}