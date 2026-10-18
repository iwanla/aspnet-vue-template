using AspNetVue.Server.Common.Results;

namespace AspNetVue.Server.Features.Identity;

/// <summary>
/// Errors owned by the Identity feature (registration and token lifecycle). Errors about the user
/// record itself — shared with other features — live in <c>Common/Errors/UserErrors.cs</c>.
/// </summary>
public static class IdentityErrors
{
    public static readonly Error EmailNotUnique = Error.Conflict(
        "Identity.EmailNotUnique",
        "The provided email is not unique");

    public static readonly Error InvalidRefreshToken = Error.Problem(
        "Identity.InvalidRefreshToken",
        "The provided refresh token is invalid or has expired");
}
