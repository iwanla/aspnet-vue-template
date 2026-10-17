using System.Net;
using AspNetVue.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetVue.Server.IntegrationTests.Authorization;

public sealed class AuthorizationTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AuthorizationTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUserById_Should_ReturnUnauthorized_WhenTokenIsMissing()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_Should_ReturnForbidden_WhenUserHasNoRoleGrantingThePermission()
    {
        // Arrange
        (Guid userId, AccessTokens tokens) = await RegisterAndLoginAsync();
        await RemoveRolesAsync(userId);
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/users/{userId}");

        // Assert: authenticated but without `users:access` -> 403, never 200.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTodos_Should_Succeed_ForARegisteredUser()
    {
        // Arrange: the seeded `user` role carries the todos permissions.
        (Guid userId, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"api/v1/todos?userId={userId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task RemoveRolesAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
