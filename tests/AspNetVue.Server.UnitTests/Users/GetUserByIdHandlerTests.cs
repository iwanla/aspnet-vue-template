using AspNetVue.Server.Authentication;
using AspNetVue.Server.Common.Results;
using AspNetVue.Server.Database;
using AspNetVue.Server.Features.Users.GetUserById;
using AspNetVue.Server.UnitTests.Abstractions;

namespace AspNetVue.Server.UnitTests.Users;

public sealed class GetUserByIdHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_PreserveUnauthorizedError_WhenUserIdDoesNotMatchContext()
    {
        await using ApplicationDbContext context = CreateDbContext();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());

        var handler = new GetUserByIdHandler(context, userContext);

        Result<GetUserByIdResponse> result = await handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Failure(
            "Users.Unauthorized", "You are not authorized to perform this action."));
    }

    [Fact]
    public async Task Handle_Should_PreserveNotFoundError_WhenUserDoesNotExist()
    {
        await using ApplicationDbContext context = CreateDbContext();
        var userId = Guid.NewGuid();
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(userId);

        var handler = new GetUserByIdHandler(context, userContext);

        Result<GetUserByIdResponse> result = await handler.Handle(
            new GetUserByIdQuery(userId), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NotFound(
            "Users.NotFound", $"The user with the Id = '{userId}' was not found"));
    }
}
