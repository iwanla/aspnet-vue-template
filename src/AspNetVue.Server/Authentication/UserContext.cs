using Microsoft.AspNetCore.Http;

namespace AspNetVue.Server.Authentication;

internal sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId =>
        UserIdOrNull ?? throw new UserContextUnavailableException();

    public Guid? UserIdOrNull =>
        _httpContextAccessor.HttpContext is { } httpContext &&
        httpContext.User.TryGetUserId(out Guid userId)
            ? userId
            : null;

    public string? Name =>
        _httpContextAccessor.HttpContext?.User.GetName();
}
