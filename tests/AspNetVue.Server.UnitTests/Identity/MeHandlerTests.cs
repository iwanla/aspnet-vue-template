using AspNetVue.Server.Authentication;
using AspNetVue.Server.Authorization;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Identity.Me;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Identity;

public sealed class MeHandlerTests : BaseHandlerTest
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Handle_Should_ReturnProfileWithEffectivePermissions()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Users.Add(new User
        {
            Id = UserId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var permissionProvider = new StubPermissionProvider("todos:read", "users:access");
        var handler = new MeHandler(context, userContext, permissionProvider);

        // Act
        Result<MeResponse> result = await handler.Handle(new MeQuery(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(UserId);
        result.Value.Email.ShouldBe("test@example.com");
        result.Value.FirstName.ShouldBe("Test");
        result.Value.LastName.ShouldBe("User");
        result.Value.Permissions.Count.ShouldBe(2);
        // Sorted so the payload is stable across calls.
        result.Value.Permissions[0].ShouldBe("todos:read");
        result.Value.Permissions[1].ShouldBe("users:access");
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(UserId);

        var handler = new MeHandler(context, userContext, new StubPermissionProvider());

        // Act
        Result<MeResponse> result = await handler.Handle(new MeQuery(), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Users.NotFound");
    }

    /// <summary>NSubstitute cannot proxy the internal <see cref="IPermissionProvider"/>.</summary>
    private sealed class StubPermissionProvider(params string[] permissions) : IPermissionProvider
    {
        public Task<IReadOnlySet<string>> GetForUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(permissions, StringComparer.Ordinal));

        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
