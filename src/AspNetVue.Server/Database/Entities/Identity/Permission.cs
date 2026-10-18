using AspNetVue.Server.Common.Domain;

namespace AspNetVue.Server.Database.Entities.Identity;

/// <summary>
/// A single permission in the form <c>&lt;feature&gt;:&lt;action&gt;</c> (for example <c>todos:create</c>).
/// Permissions are data: roles group them and users receive roles through <see cref="UserRole"/>.
/// </summary>
public sealed class Permission : BaseEntity
{
    public string Name { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = [];
}
