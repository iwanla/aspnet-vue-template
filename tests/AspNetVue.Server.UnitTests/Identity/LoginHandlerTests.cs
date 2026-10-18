using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Time;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Identity;
using AspNetVue.Server.Features.Identity.Login;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Identity;

public sealed class LoginHandlerTests : BaseHandlerTest
{
    private const string Email = "test@example.com";
    private const string Password = "Password123";

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var handler = new LoginHandler(
            context,
            Substitute.For<IPasswordHasher>(),
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound(
            "Users.NotFoundByEmail", "The user with the specified email was not found"));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPasswordIsInvalid()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        await SeedUserAsync(context);

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = new LoginHandler(
            context,
            passwordHasher,
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound(
            "Users.NotFoundByEmail", "The user with the specified email was not found"));
    }

    [Fact]
    public async Task Handle_Should_ReturnTokensAndPersistRefreshToken_WhenCredentialsAreValid()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        await SeedUserAsync(context);

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        ITokenProvider tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.Create(Arg.Any<User>()).Returns("access-token");
        tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new LoginHandler(context, passwordHasher, tokenProvider, dateTimeProvider);

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");

        RefreshToken refreshToken = await context.RefreshTokens.SingleAsync();
        refreshToken.Token.ShouldBe("refresh-token");
        refreshToken.ExpiresOnUtc.ShouldBeGreaterThan(dateTimeProvider.UtcNow);
    }

    private static async Task SeedUserAsync(ApplicationDbContext context)
    {
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = Email,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });

        await context.SaveChangesAsync();
    }
}
