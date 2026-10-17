using AspNetVue.Server.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetVue.Server.Authorization;

internal sealed class PermissionAuthorizationHandler(
    IServiceScopeFactory serviceScopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PermissionAuthorizationHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    private const string PermissionsItemKey = "AspNetVue.Server.UserPermissions";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated is not true)
        {
            // Unauthenticated requests never satisfy a permission requirement.
            return;
        }

        if (!context.User.TryGetUserId(out Guid userId))
        {
            logger.LogWarning("Authenticated principal without a valid user id claim.");
            return;
        }

        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        IReadOnlySet<string> permissions;

        if (httpContext.Items[PermissionsItemKey] is IReadOnlySet<string> cached)
        {
            // A single request may check several permissions: resolve the set once per request.
            permissions = cached;
        }
        else
        {
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                IPermissionProvider permissionProvider = scope.ServiceProvider.GetRequiredService<IPermissionProvider>();

                permissions = await permissionProvider.GetForUserIdAsync(userId, httpContext.RequestAborted);
                httpContext.Items[PermissionsItemKey] = permissions;
            }
            catch (Exception exception)
            {
                // Fail closed: never grant access when permissions cannot be resolved.
                logger.LogError(exception, "Failed to resolve permissions for user {UserId}.", userId);

                return;
            }
        }

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
