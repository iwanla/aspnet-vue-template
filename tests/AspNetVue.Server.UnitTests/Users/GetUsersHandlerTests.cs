using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Database;
using AspNetVue.Server.Database.Entities.Identity;
using AspNetVue.Server.Features.Users.GetUsers;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Users;

public sealed class GetUsersHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_ReturnPagedUsers_WhenNoneMatchTheSearch()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Users.AddRange(
            CreateUser("ada@example.com", "Ada", "Lovelace"),
            CreateUser("alan@example.com", "Alan", "Turing"),
            CreateUser("grace@example.com", "Grace", "Hopper"));
        await context.SaveChangesAsync();

        var handler = new GetUsersHandler(context);
        var query = new GetUsersQuery(Search: null, SortColumn: null, SortOrder: null, Page: 1, PageSize: 2);

        // Act
        Result<PagedList<GetUsersResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert: default sort is ascending by email.
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(3);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.Items[0].Email.ShouldBe("ada@example.com");
        result.Value.Items[1].Email.ShouldBe("alan@example.com");
    }

    [Fact]
    public async Task Handle_Should_FilterByEmailOrName_WhenSearchIsProvided()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Users.AddRange(
            CreateUser("ada@example.com", "Ada", "Lovelace"),
            CreateUser("alan@example.com", "Alan", "Turing"),
            CreateUser("grace@example.com", "Grace", "Hopper"));
        await context.SaveChangesAsync();

        var handler = new GetUsersHandler(context);
        var query = new GetUsersQuery(Search: "ace", SortColumn: null, SortOrder: null, Page: 1, PageSize: 10);

        // Act
        Result<PagedList<GetUsersResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert: "ace" matches the email of one user and the last name of another.
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Select(user => user.Email)
            .ShouldBe(["ada@example.com", "grace@example.com"], ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_Should_SortByTheRequestedColumnAndDirection()
    {
        // Arrange
        await using ApplicationDbContext context = CreateDbContext();
        context.Users.AddRange(
            CreateUser("ada@example.com", "Ada", "Lovelace"),
            CreateUser("alan@example.com", "Alan", "Turing"),
            CreateUser("grace@example.com", "Grace", "Hopper"));
        await context.SaveChangesAsync();

        var handler = new GetUsersHandler(context);
        var query = new GetUsersQuery(Search: null, SortColumn: "lastName", SortOrder: "desc", Page: 1, PageSize: 10);

        // Act
        Result<PagedList<GetUsersResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items[0].LastName.ShouldBe("Turing");
        result.Value.Items[1].LastName.ShouldBe("Lovelace");
        result.Value.Items[2].LastName.ShouldBe("Hopper");
    }

    private static User CreateUser(string email, string firstName, string lastName) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        PasswordHash = "hash"
    };
}
