using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using AspNetVue.Server.Database;

namespace AspNetVue.Server.Authorization;

internal sealed class PermissionProvider(ApplicationDbContext dbContext, HybridCache cache) : IPermissionProvider
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    private static readonly string[] EntryTags = [AuthorizationCacheKeys.PermissionsTag];

    public async Task<IReadOnlySet<string>> GetForUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        // The cache serializes values (JSON), so it stores the concrete string[] and we hand out a
        // read-only set to callers.
        string[] names = await cache.GetOrCreateAsync(
            AuthorizationCacheKeys.UserPermissions(userId),
            async token => await LoadAsync(userId, token),
            options: EntryOptions,
            tags: EntryTags,
            cancellationToken: cancellationToken);

        return names.ToHashSet(StringComparer.Ordinal);
    }

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(AuthorizationCacheKeys.UserPermissions(userId), cancellationToken);
    }

    /// <summary>One query for every permission the user has, through their roles.</summary>
    private async Task<string[]> LoadAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .SelectMany(userRole => userRole.Role.RolePermissions.Select(rolePermission => rolePermission.Permission.Name))
            .Distinct()
            .ToArrayAsync(cancellationToken);
}
