using Microsoft.AspNetCore.Builder;

namespace DavidHome.Optimizely.VirtualText.Contracts;

public interface IVirtualTextAppBuilder
{
    public IApplicationBuilder? Builder { get; set; }
}