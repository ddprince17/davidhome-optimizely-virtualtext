using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualTextBuilder
{
    public IServiceCollection? Services { get; set; }
}