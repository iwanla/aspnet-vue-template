using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Common.Errors;

/// <summary>
/// Errors about the user record itself. They live in <c>Common/</c> because more than one feature
/// reports them: the ownership/not-found checks are shared by the Todos and Users features.
/// <see cref="NotFoundByEmail"/> is reported by the login attempt and stays
/// deliberately ambiguous so it cannot be used to enumerate accounts.
/// </summary>
public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "You are not authorized to perform this action.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found");
}
