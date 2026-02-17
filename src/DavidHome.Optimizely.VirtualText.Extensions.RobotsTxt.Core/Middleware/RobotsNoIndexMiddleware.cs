using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Services;
using Microsoft.AspNetCore.Http;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Core.Middleware;

public class RobotsNoIndexMiddleware : IMiddleware
{
    private const string HeaderName = "X-Robots-Tag";

    private readonly IRobotsIndexingPolicyService _indexingPolicyService;

    public RobotsNoIndexMiddleware(IRobotsIndexingPolicyService indexingPolicyService)
    {
        _indexingPolicyService = indexingPolicyService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state;

            var robotsDirective = await _indexingPolicyService.GetRobotsDirectiveForCurrentEnvironmentAsync(httpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(robotsDirective))
            {
                return;
            }

            if (httpContext.Response.Headers.TryGetValue(HeaderName, out var existingValue))
            {
                var headerValue = existingValue.ToString();
                httpContext.Response.Headers[HeaderName] = string.IsNullOrWhiteSpace(headerValue)
                    ? robotsDirective
                    : string.Concat(headerValue, ", ", robotsDirective);
                return;
            }

            httpContext.Response.Headers[HeaderName] = robotsDirective;
        }, context);

        await next(context);
    }
}
