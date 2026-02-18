using EPiServer.DataAnnotations;
using EPiServer.Security;

namespace DavidHome.Optimizely.VirtualText.Plugin;

[PermissionTypes]
public static class PluginPermissions
{
    public const string GroupName = "VirtualText-Settings";
    public const string ViewPermissionsName = "View-Permissions";
    public const string EditPermissionsName = "Edit-Permissions";

    public static PermissionType ViewSettings { get; private set; }
    public static PermissionType EditSettings { get; private set; }

    static PluginPermissions()
    {
        ViewSettings = new PermissionType(GroupName, ViewPermissionsName);
        EditSettings = new PermissionType(GroupName, EditPermissionsName);
    }
}