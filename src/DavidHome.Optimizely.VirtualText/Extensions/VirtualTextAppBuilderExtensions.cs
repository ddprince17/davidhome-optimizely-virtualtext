using DavidHome.Optimizely.VirtualText.Contracts;
using DavidHome.Optimizely.VirtualText.Core.Controllers;
using EPiServer.Shell;

// ReSharper disable CheckNamespace

namespace Microsoft.AspNetCore.Builder;

public static class VirtualTextAppBuilderExtensions
{
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Registers Virtual Text functionality in the application.
        /// </summary>
        /// <remarks>It looks probably redundant, but I have no choice. Optimizely removes this library from MS's application parts, thus removing the
        /// <see cref="VirtualTextController"/> from being registered if it were in this library.
        /// See <see cref="ShellInitialization"/> around line 154, it removes applications parts.</remarks>
        /// <returns></returns>
        public IVirtualTextAppBuilder UseDavidHomeVirtualText()
        {
            return app.UseDavidHomeVirtualTextCore();
        }
    }
}