using DavidHome.Optimizely.VirtualText.Contracts;
using Microsoft.AspNetCore.Builder;

namespace DavidHome.Optimizely.VirtualText.Models;

internal class VirtualTextAppBuilder : IVirtualTextAppBuilder
{
    public IApplicationBuilder? Builder { get; set; }
}