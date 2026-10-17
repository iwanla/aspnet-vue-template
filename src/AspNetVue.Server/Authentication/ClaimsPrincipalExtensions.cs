using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

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

    public static string? GetName(this ClaimsPrincipal? principal)
    {
        string? name = principal?.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? principal?.FindFirstValue(ClaimTypes.Name);

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
