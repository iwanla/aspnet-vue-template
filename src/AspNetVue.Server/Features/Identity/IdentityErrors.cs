using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Identity;

/// <summary>
/// Errors owned by credential, token, and caller-session workflows.
/// </summary>
public static class IdentityErrors
{
    // Preserve the existing API error contracts independently of feature/type ownership.
    public static Error UserNotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    // Missing accounts and incorrect passwords intentionally produce the same failure.
    public static readonly Error InvalidCredentials = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found");

    public static readonly Error EmailNotUnique = Error.Conflict(
        "Identity.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Identity.InvalidRefreshToken",
        "The provided refresh token is invalid or has expired");
}
