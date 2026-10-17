using System.Security.Claims;

namespace AspNetVue.Server.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal) =>
        principal.TryGetUserId(out Guid userId) ?
            userId :
            throw new ApplicationException("User id is unavailable");

    public static bool TryGetUserId(this ClaimsPrincipal? principal, out Guid userId)
    {
        string? value = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}
