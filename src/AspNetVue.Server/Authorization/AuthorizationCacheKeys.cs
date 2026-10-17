namespace AspNetVue.Server.Authorization;

internal static class AuthorizationCacheKeys
{
    /// <summary>Tag on every cached permission set so the whole cache can be flushed at once.</summary>
    internal const string PermissionsTag = "auth:permissions";

    internal static string UserPermissions(Guid userId) => $"{PermissionsTag}:{userId}";
}
