namespace AspNetVue.Server.Features.Users;

public sealed record GetUserByEmailResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }
}
