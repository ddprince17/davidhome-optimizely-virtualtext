using Azure.Storage.Blobs;
using DavidHome.Optimizely.VirtualText.Content.AzureBlob;
using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class VirtualTextAppBuilderExtensions
{
    extension(IVirtualTextAppBuilder? app)
    {
        public IVirtualTextAppBuilder? UseAzureBlobFileStorage()
        {
            var clientFactory = app?.Builder?.ApplicationServices.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();
            var blobClient = clientFactory?.CreateClient(VirtualTextConstants.ClientName);

            // Is making sure the container is created while starting the app.
            blobClient?
                .GetBlobContainerClient(VirtualFileContentService.BlobContainerName)
                .CreateIfNotExists();

            return app;
        }
    }
}