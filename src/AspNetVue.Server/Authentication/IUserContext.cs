namespace AspNetVue.Server.Authentication;

public interface IUserContext
{
    /// <summary>The authenticated caller's id. Throws when there is no authenticated request.</summary>
    Guid UserId { get; }

    /// <summary>
    /// The authenticated caller's id, or <c>null</c> when there is no authenticated request
    /// (seeding, background work, design-time). Use this where the caller is optional, such as
    /// audit stamping.
    /// </summary>
    Guid? UserIdOrNull { get; }

    /// <summary>The authenticated caller's display name, or <c>null</c> when unavailable.</summary>
    string? Name { get; }
}
