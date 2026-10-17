namespace AspNetVue.Server.Features.Users;

/// <summary>
/// Permissions owned by the Users feature. Names are constants here (never string literals at the
/// call site) and <see cref="UsersPermissionCatalog"/> exposes them to <c>Authorization/</c>.
/// </summary>
internal static class UsersPermissions
{
    internal const string UsersAccess = "users:access";
}
