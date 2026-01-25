using DavidHome.Optimizely.VirtualText.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.Optimizely.VirtualText.Core.Models;

public class VirtualTextBuilder : IVirtualTextBuilder
{
    public IServiceCollection? Services { get; set; }
}