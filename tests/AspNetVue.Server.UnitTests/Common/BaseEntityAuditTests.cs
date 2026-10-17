using Microsoft.EntityFrameworkCore;
using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Common;

public sealed class BaseEntityAuditTests : BaseHandlerTest
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    [Fact]
    public async Task SaveChanges_Should_StampCreatedMetadata_WhenEntityIsAdded()
    {
        await using ApplicationDbContext context = CreateAuditedContext();

        context.Users.Add(NewUser());
        await context.SaveChangesAsync();

        User user = await context.Users.SingleAsync();
        user.CreatedAt.ShouldBe(Now);
        user.CreatedBy.ShouldBe(ActorId);
        user.CreatedByName.ShouldBe("Ada Lovelace");
        user.UpdatedAt.ShouldBeNull();
        user.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_Should_StampUpdatedMetadata_WhenEntityIsModified()
    {
        await using ApplicationDbContext context = CreateAuditedContext();
        context.Users.Add(NewUser());
        await context.SaveChangesAsync();

        User user = await context.Users.SingleAsync();
        user.Email = "changed@example.com";
        await context.SaveChangesAsync();

        user.UpdatedAt.ShouldBe(Now);
        user.UpdatedBy.ShouldBe(ActorId);
        user.UpdatedByName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task SaveChanges_Should_SoftDeleteAndHide_WhenEntityIsRemoved()
    {
        await using ApplicationDbContext context = CreateAuditedContext();
        context.Users.Add(NewUser());
        await context.SaveChangesAsync();

        context.Users.Remove(await context.Users.SingleAsync());
        await context.SaveChangesAsync();

        (await context.Users.CountAsync()).ShouldBe(0);

        User deleted = await context.Users.IgnoreQueryFilters().SingleAsync();
        deleted.DeletedAt.ShouldBe(Now);
        deleted.DeletedBy.ShouldBe(ActorId);
        deleted.DeletedByName.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task SaveChanges_Should_LeaveActorNull_WhenThereIsNoAuthenticatedRequest()
    {
        await using ApplicationDbContext context = CreateDbContext();

        context.Users.Add(NewUser());
        await context.SaveChangesAsync();

        User user = await context.Users.SingleAsync();
        user.CreatedBy.ShouldBeNull();
        user.CreatedByName.ShouldBeNull();
    }

    private static ApplicationDbContext CreateAuditedContext()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserIdOrNull.Returns(ActorId);
        userContext.Name.Returns("Ada Lovelace");

        return CreateDbContext(dateTimeProvider: dateTimeProvider, userContext: userContext);
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User",
        PasswordHash = "hash"
    };
}
