using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Authorization;
using AspNetVue.Server.Common.Domain;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Common.Errors;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Identity;
using AspNetVue.Server.Features.Identity.Register;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Identity;

public sealed class RegisterHandlerTests : BaseHandlerTest
{
    private static RegisterCommand Command =>
        new("test@example.com", "Test", "User", "Password123");

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenEmailIsNotUnique()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = Command.Email,
            FirstName = "Existing",
            LastName = "User",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var handler = new RegisterHandler(context, Substitute.For<IPasswordHasher>());

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(IdentityErrors.EmailNotUnique);
    }

    [Fact]
    public async Task Handle_Should_CreateUserWithHashedPasswordAndDispatchDomainEvent_WhenValid()
    {
        // Arrange
        IDomainEventsDispatcher dispatcher = Substitute.For<IDomainEventsDispatcher>();
        await using ApplicationDbContext context = CreateDbContext(dispatcher);

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash(Command.Password).Returns("hashed-password");

        var handler = new RegisterHandler(context, passwordHasher);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        User user = await context.Users.SingleAsync(u => u.Id == result.Value);
        user.Email.ShouldBe(Command.Email);
        user.PasswordHash.ShouldBe("hashed-password");
        await dispatcher.Received().DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Any(e => e is UserRegisteredDomainEvent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_AssignUserRole_WhenTheRoleExists()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        var userRole = new Role { Id = Guid.NewGuid(), Name = RoleNames.User };
        context.Roles.Add(userRole);
        await context.SaveChangesAsync();

        var handler = new RegisterHandler(context, Substitute.For<IPasswordHasher>());

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await context.UserRoles.AnyAsync(userRole => userRole.UserId == result.Value))
            .ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_AssignAdminRoleToTheFirstUserOnly()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Roles.AddRange(
            new Role { Id = Guid.NewGuid(), Name = RoleNames.User },
            new Role { Id = Guid.NewGuid(), Name = RoleNames.Admin });
        await context.SaveChangesAsync();

        var handler = new RegisterHandler(context, Substitute.For<IPasswordHasher>());

        // Act
        Result<Guid> first = await handler.Handle(
            new RegisterCommand("first@example.com", "First", "User", "Password123"),
            CancellationToken.None);
        Result<Guid> second = await handler.Handle(
            new RegisterCommand("second@example.com", "Second", "User", "Password123"),
            CancellationToken.None);

        // Assert
        Guid adminRoleId = await context.Roles
            .Where(role => role.Name == RoleNames.Admin)
            .Select(role => role.Id)
            .SingleAsync();

        (await context.UserRoles.AnyAsync(userRole => userRole.UserId == first.Value && userRole.RoleId == adminRoleId))
            .ShouldBeTrue();
        (await context.UserRoles.AnyAsync(userRole => userRole.UserId == second.Value && userRole.RoleId == adminRoleId))
            .ShouldBeFalse();
    }
}
