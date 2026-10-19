using AspNetVue.Server.Authorization;
using AspNetVue.Server.Common.Domain;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetVue.Server.IntegrationTests.Authorization;

public sealed class JoinQueryFilterTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public JoinQueryFilterTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("none", 1, 1)]
    [InlineData("user", 0, 1)]
    [InlineData("role", 0, 0)]
    [InlineData("permission", 1, 0)]
    public async Task Queries_Should_ConsistentlyHideJoinsWithDeletedPrincipals(
        string deletedPrincipal, int expectedUserRoles, int expectedRolePermissions)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = UniqueEmail(),
            FirstName = "Filter",
            LastName = "Test",
            PasswordHash = "hash"
        };
        var role = new Role { Id = Guid.NewGuid(), Name = $"filter-role-{Guid.NewGuid():N}" };
        var permission = new Permission { Id = Guid.NewGuid(), Name = $"filter:permission-{Guid.NewGuid():N}" };
        context.UserRoles.Add(new UserRole { User = user, Role = role });
        context.RolePermissions.Add(new RolePermission { Role = role, Permission = permission });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Load only the principal so tracked-dependent cascade deletion cannot hide a filter bug.
        BaseEntity? principal = deletedPrincipal switch
        {
            "user" => await context.Users.SingleAsync(candidate => candidate.Id == user.Id),
            "role" => await context.Roles.SingleAsync(candidate => candidate.Id == role.Id),
            "permission" => await context.Permissions.SingleAsync(candidate => candidate.Id == permission.Id),
            _ => null
        };
        if (principal is not null)
        {
            context.Remove(principal);
            await context.SaveChangesAsync();
            principal.DeletedAt.ShouldNotBeNull();
        }
        context.ChangeTracker.Clear();

        IQueryable<UserRole> userRoles = context.UserRoles.AsNoTracking()
            .Where(join => join.UserId == user.Id);
        IQueryable<RolePermission> rolePermissions = context.RolePermissions.AsNoTracking()
            .Where(join => join.RoleId == role.Id);

        (await userRoles.CountAsync()).ShouldBe(expectedUserRoles);
        (await userRoles.Include(join => join.User).Include(join => join.Role).ToListAsync())
            .Count.ShouldBe(expectedUserRoles);
        (await userRoles.Select(join => join.Role.Name).ToListAsync()).Count.ShouldBe(expectedUserRoles);

        (await rolePermissions.CountAsync()).ShouldBe(expectedRolePermissions);
        (await rolePermissions.Include(join => join.Role).Include(join => join.Permission).ToListAsync())
            .Count.ShouldBe(expectedRolePermissions);
        (await rolePermissions.Select(join => join.Permission.Name).ToListAsync()).Count.ShouldBe(expectedRolePermissions);

        // Filters hide joins rather than deleting them; explicit audit queries can still see both.
        (await userRoles.IgnoreQueryFilters().CountAsync()).ShouldBe(1);
        (await rolePermissions.IgnoreQueryFilters().CountAsync()).ShouldBe(1);

        // Fresh, isolated user: no cached grant may bypass this database read.
        IPermissionProvider provider = scope.ServiceProvider.GetRequiredService<IPermissionProvider>();
        IReadOnlySet<string> permissions = await provider.GetForUserIdAsync(user.Id, CancellationToken.None);
        if (deletedPrincipal == "none")
        {
            permissions.ShouldContain(permission.Name);
        }
        else
        {
            permissions.ShouldBeEmpty();
        }
    }
}
