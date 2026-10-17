using AspNetVue.Server.Authorization;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.UnitTests.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace AspNetVue.Server.UnitTests.Authorization;

public sealed class PermissionProviderTests : BaseHandlerTest
{
    [Fact]
    public async Task GetForUserIdAsync_Should_ReturnPermissionsGrantedThroughRoles()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        User user = await AddUserWithPermissionsAsync(context, "todos:read", "users:access");

        var provider = new PermissionProvider(context, CreateCache());

        // Act
        IReadOnlySet<string> permissions = await provider.GetForUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        permissions.ShouldBe(["todos:read", "users:access"], ignoreOrder: true);
    }

    [Fact]
    public async Task GetForUserIdAsync_Should_ReturnEmptySet_WhenUserHasNoRoles()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "lonely@example.com",
            FirstName = "Lonely",
            LastName = "User",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var provider = new PermissionProvider(context, CreateCache());

        // Act
        IReadOnlySet<string> permissions = await provider.GetForUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetForUserIdAsync_Should_ServeCachedValue_UntilInvalidated()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        User user = await AddUserWithPermissionsAsync(context, "todos:read");

        HybridCache cache = CreateCache();
        var provider = new PermissionProvider(context, cache);

        await provider.GetForUserIdAsync(user.Id, CancellationToken.None);

        // Act: grant an extra permission through the role, the short TTL cache should hide it...
        await GrantPermissionAsync(context, user, "todos:delete");
        IReadOnlySet<string> cached = await provider.GetForUserIdAsync(user.Id, CancellationToken.None);

        // ...until the cache is explicitly invalidated.
        await provider.InvalidateAsync(user.Id, CancellationToken.None);
        IReadOnlySet<string> refreshed = await provider.GetForUserIdAsync(user.Id, CancellationToken.None);

        // Assert
        cached.ShouldNotContain("todos:delete");
        refreshed.ShouldContain("todos:delete");
    }

    private static async Task<User> AddUserWithPermissionsAsync(
        ApplicationDbContext context,
        params string[] permissionNames)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        };

        var role = new Role { Id = Guid.NewGuid(), Name = $"role-{Guid.NewGuid():N}" };

        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });

        foreach (string permissionName in permissionNames)
        {
            var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };

            context.Permissions.Add(permission);
            context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Role = role,
                Permission = permission
            });
        }

        await context.SaveChangesAsync();

        return user;
    }

    private static async Task GrantPermissionAsync(ApplicationDbContext context, User user, string permissionName)
    {
        UserRole userRole = await context.UserRoles.SingleAsync(candidate => candidate.UserId == user.Id);
        var permission = new Permission { Id = Guid.NewGuid(), Name = permissionName };

        context.Permissions.Add(permission);
        context.RolePermissions.Add(new RolePermission { RoleId = userRole.RoleId, PermissionId = permission.Id });

        await context.SaveChangesAsync();
    }
}
