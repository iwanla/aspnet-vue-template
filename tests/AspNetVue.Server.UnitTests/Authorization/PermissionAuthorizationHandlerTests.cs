using System.Security.Claims;
using AspNetVue.Server.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspNetVue.Server.UnitTests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    private static readonly PermissionRequirement Requirement = new("todos:read");

    [Fact]
    public async Task HandleAsync_Should_NotSucceed_WhenUserIsAnonymous()
    {
        // Arrange
        (PermissionAuthorizationHandler handler, StubPermissionProvider provider) = CreateHandler("todos:read");
        AuthorizationHandlerContext context = ContextFor(Requirement, new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
        provider.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_WhenUserHoldsThePermission()
    {
        // Arrange
        (PermissionAuthorizationHandler handler, _) = CreateHandler("todos:read");
        AuthorizationHandlerContext context = ContextFor(Requirement, AuthenticatedUser(Guid.NewGuid()));

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_NotSucceed_WhenUserLacksThePermission()
    {
        // Arrange
        (PermissionAuthorizationHandler handler, _) = CreateHandler("users:access");
        AuthorizationHandlerContext context = ContextFor(Requirement, AuthenticatedUser(Guid.NewGuid()));

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_NotSucceed_WhenTheProviderFails()
    {
        // Arrange
        (PermissionAuthorizationHandler handler, _) = CreateHandler("todos:read", throwOnRead: true);
        AuthorizationHandlerContext context = ContextFor(Requirement, AuthenticatedUser(Guid.NewGuid()));

        // Act
        await handler.HandleAsync(context);

        // Assert: fail closed, an unavailable permission store must not grant access.
        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_Should_ResolvePermissionsOncePerRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        (PermissionAuthorizationHandler handler, StubPermissionProvider provider) = CreateHandler("todos:read");

        AuthorizationHandlerContext readContext = ContextFor(Requirement, AuthenticatedUser(userId));
        AuthorizationHandlerContext accessContext = ContextFor(new PermissionRequirement("users:access"), AuthenticatedUser(userId));

        // Act
        await handler.HandleAsync(readContext);
        await handler.HandleAsync(accessContext);

        // Assert
        provider.CallCount.ShouldBe(1);
        readContext.HasSucceeded.ShouldBeTrue();
        accessContext.HasSucceeded.ShouldBeFalse();
    }

    private static AuthorizationHandlerContext ContextFor(PermissionRequirement requirement, ClaimsPrincipal user) =>
        new([requirement], user, resource: null);

    private static ClaimsPrincipal AuthenticatedUser(Guid userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], authenticationType: "test"));

    private static (PermissionAuthorizationHandler Handler, StubPermissionProvider Provider) CreateHandler(
        string permission,
        bool throwOnRead = false)
    {
        var provider = new StubPermissionProvider(permission, throwOnRead);

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IPermissionProvider)).Returns(provider);

        IServiceScope scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        IServiceScopeFactory scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var handler = new PermissionAuthorizationHandler(
            scopeFactory,
            httpContextAccessor,
            NullLogger<PermissionAuthorizationHandler>.Instance);

        return (handler, provider);
    }

    private sealed class StubPermissionProvider(string permission, bool throwOnRead) : IPermissionProvider
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlySet<string>> GetForUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            CallCount++;

            if (throwOnRead)
            {
                throw new InvalidOperationException("Permission store is unavailable.");
            }

            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>([permission], StringComparer.Ordinal));
        }

        public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
