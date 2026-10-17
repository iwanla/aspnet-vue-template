namespace AspNetVue.Server.Database.Entities.Identity;

/// <summary>
/// An administrative grouping of permissions. Authorization only ever checks permissions, never
/// role names, so a role is just a convenient way to hand out several permissions at once.
/// Lives in <c>Database/</c> because <c>Authorization/</c> consumes it.
/// </summary>
public sealed class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = [];
}
