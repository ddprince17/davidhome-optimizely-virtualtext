using EPiServer.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace DavidHome.Optimizely.VirtualText.Plugin;

/// <summary>
/// Allows access authorization to any of the specified name under a certain group.
/// This class is highly inspired from <see cref="EPiServer.Web.Mvc.AuthorizePermissionAttribute"/>.
/// </summary>
public class AnyAuthorizePermissionAttribute : ActionFilterAttribute, IAuthorizationFilter
{
    private readonly IReadOnlyCollection<PermissionType> _permissions;
    
    public AnyAuthorizePermissionAttribute(string groupName, params string[] names)
    {
        _permissions = names.Select(name => new PermissionType(groupName, name)).ToArray();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();
        var user = context.HttpContext.User;

        if (_permissions.Any(permission => permissionService.IsPermitted(user, permission)))
        {
            return;
        }

        var userAuthenticated = user.Identity?.IsAuthenticated ?? false;
        
        context.Result = userAuthenticated ? new ForbidResult() : new ChallengeResult();
    }
}