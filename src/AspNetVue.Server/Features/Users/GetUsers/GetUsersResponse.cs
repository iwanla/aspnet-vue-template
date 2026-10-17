namespace AspNetVue.Server.Features.Users.GetUsers;

public sealed record GetUsersResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }
}
