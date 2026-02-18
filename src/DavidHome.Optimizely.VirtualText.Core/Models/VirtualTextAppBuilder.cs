using DavidHome.Optimizely.VirtualText.Contracts;
using Microsoft.AspNetCore.Builder;

namespace DavidHome.Optimizely.VirtualText.Core.Models;

internal class VirtualTextAppBuilder : IVirtualTextAppBuilder
{
    public IApplicationBuilder? Builder { get; set; }
}