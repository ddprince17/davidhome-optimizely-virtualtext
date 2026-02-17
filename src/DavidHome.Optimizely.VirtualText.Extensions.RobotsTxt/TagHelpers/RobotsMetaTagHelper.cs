using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.TagHelpers;

[HtmlTargetElement("meta", Attributes = NameAttributeName)]
public class RobotsMetaTagHelper : TagHelper
{
    private const string NameAttributeName = "name";
    private const string ContentAttributeName = "content";

    private readonly IRobotsIndexingPolicyService _indexingPolicyService;

    [HtmlAttributeName(NameAttributeName)]
    public string? Name { get; set; }

    public RobotsMetaTagHelper(IRobotsIndexingPolicyService indexingPolicyService)
    {
        _indexingPolicyService = indexingPolicyService;
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!string.Equals(Name, "robots", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var robotsDirective = await _indexingPolicyService.GetRobotsDirectiveForCurrentEnvironmentAsync();
        if (string.IsNullOrWhiteSpace(robotsDirective))
        {
            return;
        }

        output.Attributes.SetAttribute(ContentAttributeName, robotsDirective);
    }
}
