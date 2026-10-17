namespace AspNetVue.Server.Database.Entities.Identity;

/// <summary>Join entity between <see cref="Role"/> and <see cref="Permission"/>.</summary>
public sealed class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public Role Role { get; set; }

    public Permission Permission { get; set; }
}
