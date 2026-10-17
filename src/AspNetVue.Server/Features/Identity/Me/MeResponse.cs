namespace AspNetVue.Server.Features.Identity.Me;

public sealed record MeResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    /// <summary>
    /// Effective permissions of the caller, resolved from the database (never from the JWT) so a
    /// client can hide the UI it is not allowed to use. The endpoint authority is still the server.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; init; } = [];
}
