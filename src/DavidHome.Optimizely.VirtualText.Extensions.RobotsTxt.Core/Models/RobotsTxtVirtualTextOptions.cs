using DavidHome.Optimizely.VirtualText.Models;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Models;

public class RobotsTxtVirtualTextOptions : VirtualTextOptions
{
    public RobotsTxtOptions RobotsTxt { get; set; } = new();
}

public class RobotsTxtOptions
{
    public bool DisableRobotsTxtManipulator { get; set; } = false;
}