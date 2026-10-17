using AspNetVue.Server.Authorization;

namespace AspNetVue.Server.Features.Users;

/// <summary>Exposes the Users permissions to the assembly scan in <c>Authorization/</c>.</summary>
internal sealed class UsersPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> Permissions { get; } =
    [
        new(UsersPermissions.UsersAccess, [])
    ];
}
