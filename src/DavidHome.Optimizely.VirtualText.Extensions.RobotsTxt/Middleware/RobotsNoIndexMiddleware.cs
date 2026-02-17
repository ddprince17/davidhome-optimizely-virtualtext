using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Contracts;
using DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Services;
using Microsoft.AspNetCore.Http;

namespace DavidHome.Optimizely.VirtualText.Extensions.RobotsTxt.Middleware;

public class RobotsNoIndexMiddleware
{
    private const string HeaderName = "X-Robots-Tag";

    private readonly RequestDelegate _next;

    public RobotsNoIndexMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRobotsIndexingPolicyService indexingPolicyService)
    {
        context.Response.OnStarting(async state =>
        {
            var httpContext = (HttpContext)state;

            var allowIndexing = await indexingPolicyService.ShouldAllowIndexingCurrentEnvironmentAsync(httpContext.RequestAborted);
            if (allowIndexing)
            {
                return;
            }

            if (httpContext.Response.Headers.TryGetValue(HeaderName, out var existingValue))
            {
                var headerValue = existingValue.ToString();
                if (headerValue.Contains("noindex", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                httpContext.Response.Headers[HeaderName] = string.Concat(headerValue, ", ", RobotsTxtConstants.NoIndexDirective);
                return;
            }

            httpContext.Response.Headers[HeaderName] = RobotsTxtConstants.NoIndexDirective;
        }, context);

        await _next(context);
    }
}
