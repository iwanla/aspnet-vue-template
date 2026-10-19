namespace AspNetVue.Server.Database.Entities.Identity;

/// <summary>
/// Join entity between a user and a <see cref="Role"/>. When the application becomes multi-tenant,
/// add the tenant column here and filter every permission query by it.
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public User User { get; set; }

    public Role Role { get; set; }
}
