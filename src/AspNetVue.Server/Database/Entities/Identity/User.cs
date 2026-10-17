using AspNetVue.Server.Common;

namespace AspNetVue.Server.Database.Entities.Identity;

/// <summary>
/// Identity entity, consumed by <c>Authentication/</c> and <c>Authorization/</c> as well as by
/// features, so it lives outside <c>Features/</c>. Features that need one own profile data store
/// it in their own entity keyed by <see cref="Id"/> instead of extending this type.
/// </summary>
public sealed class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
}
