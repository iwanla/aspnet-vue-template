using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Users;

/// <summary>
/// Errors owned by user-record queries. Other features define their own failures
/// rather than depending on the Users feature.
/// </summary>
public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");
}
